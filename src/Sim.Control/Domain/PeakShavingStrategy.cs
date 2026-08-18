using Sim.SharedKernel;

namespace Sim.Control.Domain;

/// <summary>
/// Adaptive peak shaving: discharge above the 80th percentile of the load
/// observed over a rolling day, recharge below the 40th, idle in between. An
/// optional hard ceiling applies on top for contractual connection limits. A
/// fixed threshold was tried first and measurably failed - drained before the
/// evening peak, 0 kW shaved (ADR-0010). With no observed history nothing can
/// be called a peak, so the very first decision is always idle. Every setpoint
/// is limited by the rating and by what the cells can actually absorb or
/// deliver, so the controller never commands the impossible.
/// </summary>
public sealed class PeakShavingStrategy : IStorageControlStrategy
{
    public const double DischargePercentile = 0.80;
    public const double RechargePercentile = 0.40;

    private const int WindowSize = 96;
    private const double SmallestMeaningfulCommandKw = 0.001;

    private readonly Queue<double> _recentLoadsKw = new();
    private readonly double _legEfficiency;
    private readonly double? _hardCeilingKw;

    public PeakShavingStrategy(double? fixedThresholdKw = null, double roundTripEfficiency = 0.9)
    {
        _hardCeilingKw = fixedThresholdKw;
        _legEfficiency = Math.Sqrt(Math.Clamp(roundTripEfficiency, 0.1, 1.0));
    }

    private void ObserveTheLoad(double netKw)
    {
        _recentLoadsKw.Enqueue(netKw);
        while (_recentLoadsKw.Count > WindowSize) _recentLoadsKw.Dequeue();
    }

    private void RecalculateTheThresholds()
    {
        var sortedLoadsKw = _recentLoadsKw.ToArray();
        Array.Sort(sortedLoadsKw);
        DischargeThresholdKw = PickThePercentile(sortedLoadsKw, DischargePercentile);
        RechargeThresholdKw = PickThePercentile(sortedLoadsKw, RechargePercentile);
    }

    private static double PickThePercentile(double[] sortedLoadsKw, double fraction)
    {
        var position = fraction * (sortedLoadsKw.Length - 1);
        var index = Math.Clamp((int)Math.Round(position), 0, sortedLoadsKw.Length - 1);
        return sortedLoadsKw[index];
    }

    private double LowerTheCeilingToTheHardLimit() =>
        _hardCeilingKw is { } hardKw ? Math.Min(DischargeThresholdKw, hardKw) : DischargeThresholdKw;

    private StorageSetpoint DischargeDownToTheCeiling(GridState state, double netKw, double ceilingKw, double hours)
    {
        var wantedKw = netKw - ceilingKw;
        var deliverableKw = state.StateOfChargeKwh * _legEfficiency / hours;
        var dischargeKw = Math.Min(Math.Min(wantedKw, state.MaxPowerKw), Math.Max(0, deliverableKw));
        return dischargeKw <= SmallestMeaningfulCommandKw
            ? StorageSetpoint.Idle
            : new StorageSetpoint(new Kilowatts(-dischargeKw));
    }

    private StorageSetpoint RechargeWithinTheHeadroom(GridState state, double netKw, double hours)
    {
        var headroomKw = RechargeThresholdKw - netKw;
        var absorbableKw = (state.CapacityKwh - state.StateOfChargeKwh) / _legEfficiency / hours;
        var chargeKw = Math.Min(Math.Min(headroomKw, state.MaxPowerKw), Math.Max(0, absorbableKw));
        return chargeKw <= SmallestMeaningfulCommandKw
            ? StorageSetpoint.Idle
            : new StorageSetpoint(new Kilowatts(chargeKw));
    }

    public string Name => _hardCeilingKw is { } hardKw
        ? $"Peak shaving: top {(1 - DischargePercentile) * 100:F0}% of load, hard ceiling {hardKw:F0} kW"
        : $"Peak shaving: top {(1 - DischargePercentile) * 100:F0}% of load";

    public double DischargeThresholdKw { get; private set; }
    public double RechargeThresholdKw { get; private set; }

    public StorageSetpoint Decide(GridState state, TimeSpan duration)
    {
        var netKw = state.NetLoadBeforeStorage.Value;
        ObserveTheLoad(netKw);
        RecalculateTheThresholds();

        var ceilingKw = LowerTheCeilingToTheHardLimit();
        if (netKw > ceilingKw) return DischargeDownToTheCeiling(state, netKw, ceilingKw, duration.TotalHours);
        if (netKw < RechargeThresholdKw) return RechargeWithinTheHeadroom(state, netKw, duration.TotalHours);
        return StorageSetpoint.Idle;
    }
}
