using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;

namespace Sim.Simulation.Domain;

/// <summary>
/// How the battery physically responds to a command; leaf of the
/// <see cref="SimulationRun"/> aggregate. Loss model (A-010): losses split
/// evenly across the two legs - each leg keeps sqrt(roundTrip) of what crosses
/// it, so a full round trip keeps exactly roundTrip. On charge the meter pays
/// the loss; on discharge the cells pay it. With real hardware this class
/// disappears and state of charge arrives as telemetry.
/// </summary>
public sealed class BatterySimulator
{
    private readonly Battery _battery;
    private readonly double _legEfficiency;

    private double _stateOfChargeKwh;

    public BatterySimulator(Battery battery)
    {
        _battery = battery;
        _legEfficiency = Math.Sqrt(Math.Clamp(battery.RoundTripEfficiency, 0.1, 1.0));
        _stateOfChargeKwh = battery.CapacityKwh / 2;
    }

    private double ClampToThePowerRating(StorageSetpoint setpoint) =>
        Math.Clamp(setpoint.Power.Value, -_battery.MaxPowerKw, _battery.MaxPowerKw);

    private double Charge(double commandedKw, TimeSpan duration)
    {
        var meteredKwh = Math.Min(commandedKw * duration.TotalHours, ConvertRoomLeftToMeteredKwh(_stateOfChargeKwh));
        StoreInTheCells(meteredKwh);
        return AverageOverTheInterval(meteredKwh, duration);
    }

    private double Discharge(double commandedKw, TimeSpan duration)
    {
        var meteredKwh = Math.Min(commandedKw * duration.TotalHours, ConvertCellContentToMeteredKwh(_stateOfChargeKwh));
        TakeFromTheCells(meteredKwh);
        return -AverageOverTheInterval(meteredKwh, duration);
    }

    private double ConvertRoomLeftToMeteredKwh(double stateOfChargeKwh) =>
        (_battery.CapacityKwh - stateOfChargeKwh) / _legEfficiency;

    private double ConvertCellContentToMeteredKwh(double stateOfChargeKwh) =>
        stateOfChargeKwh * _legEfficiency;

    private void StoreInTheCells(double meteredKwh) =>
        _stateOfChargeKwh = ClampToTheCells(_stateOfChargeKwh + meteredKwh * _legEfficiency);

    private void TakeFromTheCells(double meteredKwh) =>
        _stateOfChargeKwh = ClampToTheCells(_stateOfChargeKwh - meteredKwh / _legEfficiency);

    private double ClampToTheCells(double chargeKwh) =>
        Math.Clamp(chargeKwh, 0, _battery.CapacityKwh);

    private static double AverageOverTheInterval(double meteredKwh, TimeSpan duration) =>
        meteredKwh / duration.TotalHours;

    public double StateOfChargeKwh => _stateOfChargeKwh;
    public double CapacityKwh => _battery.CapacityKwh;
    public double StateOfChargePercent => 100.0 * _stateOfChargeKwh / _battery.CapacityKwh;

    public PowerReading Apply(StorageSetpoint setpoint, DateTimeOffset instant, TimeSpan duration)
    {
        var commandedKw = ClampToThePowerRating(setpoint);

        var meteredKw = commandedKw switch
        {
            > 0 => Charge(commandedKw, duration),
            < 0 => Discharge(-commandedKw, duration),
            _ => 0.0,
        };

        return new PowerReading(_battery.MeterId, instant, new Kilowatts(meteredKw));
    }
}
