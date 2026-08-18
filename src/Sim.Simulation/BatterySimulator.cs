using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;

namespace Sim.Simulation;

/// <summary>
/// Simulates how the battery physically responds to a command: it clamps the
/// setpoint to the power rating and to the energy actually available or free,
/// applies round-trip losses, and tracks state of charge.
///
/// With real hardware this class disappears and state of charge arrives as
/// telemetry. The controller that produced the setpoint does NOT disappear -
/// which is exactly why control lives in its own context and this does not.
/// </summary>
public sealed class BatterySimulator(Battery battery)
{
    private readonly double _legEfficiency = Math.Sqrt(Math.Clamp(battery.RoundTripEfficiency, 0.1, 1.0));

    /// <summary>Starts half full so peak shaving has something to give on the first peak.</summary>
    public double StateOfChargeKwh { get; private set; } = battery.CapacityKwh / 2;

    public double CapacityKwh => battery.CapacityKwh;
    public double StateOfChargePercent => 100.0 * StateOfChargeKwh / battery.CapacityKwh;

    /// <summary>Applies a command and reports what the battery's meter actually saw.</summary>
    public PowerReading Apply(StorageSetpoint setpoint, DateTimeOffset instant, TimeSpan duration)
    {
        var hours = duration.TotalHours;
        var commanded = Math.Clamp(setpoint.Power.Value, -battery.MaxPowerKw, battery.MaxPowerKw);

        double actual;
        if (commanded > 0)
        {
            // Charging: the meter sees more than the cells store, losses included.
            var free = battery.CapacityKwh - StateOfChargeKwh;
            var meteredKwh = Math.Min(commanded * hours, free / _legEfficiency);
            StateOfChargeKwh += meteredKwh * _legEfficiency;
            actual = meteredKwh / hours;
        }
        else if (commanded < 0)
        {
            // Discharging: the cells give up more than the meter delivers.
            var deliverableKwh = Math.Min(-commanded * hours, StateOfChargeKwh * _legEfficiency);
            StateOfChargeKwh -= deliverableKwh / _legEfficiency;
            actual = -deliverableKwh / hours;
        }
        else actual = 0;

        StateOfChargeKwh = Math.Clamp(StateOfChargeKwh, 0, battery.CapacityKwh);
        return new PowerReading(battery.MeterId, instant, new Kilowatts(actual));
    }
}
