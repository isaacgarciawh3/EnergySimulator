using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;

namespace Sim.Simulation.Domain;

/// <summary>
/// Simulates how the battery physically responds to a command. Leaf of the
/// <see cref="SimulationRun"/> aggregate.
///
/// With real hardware this class disappears and state of charge arrives as
/// telemetry. The controller that produced the setpoint does NOT disappear -
/// which is exactly why control lives in its own context and this does not.
/// </summary>
public sealed class BatterySimulator(Battery battery)
{
    /// <summary>
    /// THE LOSS MODEL (A-010): losses are split evenly across the two legs.
    /// Each leg - charging into the cells, discharging out of them - keeps
    /// sqrt(roundTrip) of the energy that crosses it, so a full round trip
    /// keeps exactly roundTrip. The clamp guards against a nonsense efficiency
    /// ever reaching the physics.
    /// </summary>
    private readonly double _legEfficiency = Math.Sqrt(Math.Clamp(battery.RoundTripEfficiency, 0.1, 1.0));

    /// <summary>Starts half full so peak shaving has something to give on the first peak (A-010).</summary>
    public double StateOfChargeKwh { get; private set; } = battery.CapacityKwh / 2;

    public double CapacityKwh => battery.CapacityKwh;
    public double StateOfChargePercent => 100.0 * StateOfChargeKwh / battery.CapacityKwh;

    /// <summary>
    /// Applies a command and reports what the battery's meter actually saw.
    /// The process: the rating clamps the request, the appropriate leg moves
    /// the energy, and the meter reports the interval-average power.
    /// </summary>
    public PowerReading Apply(StorageSetpoint setpoint, DateTimeOffset instant, TimeSpan duration)
    {
        var commandedKw = ClampedToThePowerRating(setpoint);

        var meteredKw = commandedKw switch
        {
            > 0 => Charge(commandedKw, duration),
            < 0 => Discharge(-commandedKw, duration),
            _ => 0.0,
        };

        StateOfChargeKwh = BoundedByThePhysicalCells(StateOfChargeKwh);
        return new PowerReading(battery.MeterId, instant, new Kilowatts(meteredKw));
    }

    /// <summary>RULE: a setpoint is a request; the power rating is the law, in both directions.</summary>
    private double ClampedToThePowerRating(StorageSetpoint setpoint) =>
        Math.Clamp(setpoint.Power.Value, -battery.MaxPowerKw, battery.MaxPowerKw);

    /// <summary>
    /// RULE: on charge, the METER pays for the losses - the meter sees more
    /// energy than the cells store. Delivery stops when the cells are full.
    /// </summary>
    private double Charge(double commandedKw, TimeSpan duration)
    {
        var meteredKwh = Math.Min(commandedKw * duration.TotalHours, RoomLeftInMeteredEnergyKwh);
        StateOfChargeKwh += StoredInTheCellsFrom(meteredKwh);
        return AveragePowerOverTheInterval(meteredKwh, duration);
    }

    /// <summary>
    /// RULE: on discharge, the CELLS pay for the losses - they give up more
    /// energy than the meter delivers. Delivery stops when the cells are empty.
    /// Negative at the meter: discharging is generation (ADR-0002).
    /// </summary>
    private double Discharge(double commandedKw, TimeSpan duration)
    {
        var meteredKwh = Math.Min(commandedKw * duration.TotalHours, DeliverableMeteredEnergyKwh);
        StateOfChargeKwh -= GivenUpByTheCellsFor(meteredKwh);
        return -AveragePowerOverTheInterval(meteredKwh, duration);
    }

    /// <summary>How much the meter can still push before the cells are full, losses included.</summary>
    private double RoomLeftInMeteredEnergyKwh => (battery.CapacityKwh - StateOfChargeKwh) / _legEfficiency;

    /// <summary>How much the meter can still receive before the cells are empty, losses included.</summary>
    private double DeliverableMeteredEnergyKwh => StateOfChargeKwh * _legEfficiency;

    /// <summary>The charging leg keeps its share: the cells store less than the meter saw.</summary>
    private double StoredInTheCellsFrom(double meteredKwh) => meteredKwh * _legEfficiency;

    /// <summary>The discharging leg takes its share: the cells give up more than the meter delivers.</summary>
    private double GivenUpByTheCellsFor(double meteredKwh) => meteredKwh / _legEfficiency;

    /// <summary>RULE: reported power is the interval average, so the final partial interval accounts exactly (A-004 for the EV, same honesty here).</summary>
    private static double AveragePowerOverTheInterval(double meteredKwh, TimeSpan duration) =>
        meteredKwh / duration.TotalHours;

    /// <summary>
    /// INVARIANT GUARD, not physics: the arithmetic above cannot overshoot by
    /// more than floating point dust, and this pins that dust inside the cells.
    /// </summary>
    private double BoundedByThePhysicalCells(double chargeKwh) =>
        Math.Clamp(chargeKwh, 0, battery.CapacityKwh);
}
