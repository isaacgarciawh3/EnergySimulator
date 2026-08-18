using Sim.SharedKernel;

namespace Sim.Control.Domain;

/// <summary>
/// Threshold peak shaving. Above the threshold the battery discharges to pull
/// the neighbourhood back down to it; well below the threshold it recharges so
/// it has something to give at the next peak.
///
/// The controller is clamped by what is physically possible - power rating and
/// the energy actually available in, or free in, the battery over this interval
/// - so it can never command something the hardware cannot do. Charging is
/// limited to headroom below <see cref="RechargeFraction"/> of the threshold so
/// that recharging never itself creates the peak it is meant to prevent.
/// </summary>
public sealed class PeakShavingStrategy(double thresholdKw, double roundTripEfficiency = 0.9) : IStorageControlStrategy
{
    /// <summary>Recharge only while net load sits below this fraction of the threshold.</summary>
    public const double RechargeFraction = 0.6;

    public string Name => $"Peak shaving above {thresholdKw:F0} kW";
    public double ThresholdKw => thresholdKw;

    public StorageSetpoint Decide(GridState state, TimeSpan duration)
    {
        var hours = duration.TotalHours;
        var net = state.NetLoadBeforeStorage.Value;
        var chargeEfficiency = Math.Sqrt(Math.Clamp(roundTripEfficiency, 0.1, 1.0));

        if (net > thresholdKw)
        {
            var wanted = net - thresholdKw;
            var deliverable = state.StateOfChargeKwh * chargeEfficiency / hours;
            var discharge = Math.Min(Math.Min(wanted, state.MaxPowerKw), Math.Max(0, deliverable));
            return discharge <= 0 ? StorageSetpoint.Idle : new StorageSetpoint(new Kilowatts(-discharge));
        }

        var rechargeCeiling = thresholdKw * RechargeFraction;
        if (net < rechargeCeiling)
        {
            var headroom = rechargeCeiling - net;
            var absorbable = (state.CapacityKwh - state.StateOfChargeKwh) / chargeEfficiency / hours;
            var charge = Math.Min(Math.Min(headroom, state.MaxPowerKw), Math.Max(0, absorbable));
            return charge <= 0 ? StorageSetpoint.Idle : new StorageSetpoint(new Kilowatts(charge));
        }

        return StorageSetpoint.Idle;
    }
}
