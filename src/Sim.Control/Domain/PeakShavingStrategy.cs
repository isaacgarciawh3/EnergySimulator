using Sim.SharedKernel;

namespace Sim.Control.Domain;

/// <summary>
/// Adaptive peak shaving. The battery discharges during the highest load
/// periods and recharges during the lowest, where "highest" and "lowest" are
/// percentiles of the load actually observed over a rolling window rather than
/// fixed numbers.
///
/// A fixed threshold was tried first and failed in a way worth recording: at
/// 45 kW against a winter load that sits above 45 kW for most of the day, the
/// battery discharged continuously from the first interval, hit empty long
/// before the evening peak, and delivered a 0 kW peak reduction. It was working
/// exactly as instructed and was useless, because the threshold had no relation
/// to the load it was meant to shave.
///
/// Percentiles fix that by definition: the top band is always a small minority
/// of intervals, whatever the season, so there is always energy left for it.
/// </summary>
public sealed class PeakShavingStrategy : IStorageControlStrategy
{
    /// <summary>Discharge above this percentile of recent load.</summary>
    public const double DischargePercentile = 0.80;

    /// <summary>Recharge below this percentile of recent load.</summary>
    public const double RechargePercentile = 0.40;

    private const int WindowSize = 96; // one simulated day at the default tick

    private readonly Queue<double> _recent = new();
    private readonly double _legEfficiency;
    private readonly double? _fixedThresholdKw;

    /// <param name="fixedThresholdKw">
    /// Optional hard ceiling. When set, the battery also discharges to hold the
    /// neighbourhood below it, on top of the percentile behaviour.
    /// </param>
    public PeakShavingStrategy(double? fixedThresholdKw = null, double roundTripEfficiency = 0.9)
    {
        _fixedThresholdKw = fixedThresholdKw;
        _legEfficiency = Math.Sqrt(Math.Clamp(roundTripEfficiency, 0.1, 1.0));
    }

    public string Name => _fixedThresholdKw is { } t
        ? $"Peak shaving: top {(1 - DischargePercentile) * 100:F0}% of load, hard ceiling {t:F0} kW"
        : $"Peak shaving: top {(1 - DischargePercentile) * 100:F0}% of load";

    public double DischargeThresholdKw { get; private set; }
    public double RechargeThresholdKw { get; private set; }

    public StorageSetpoint Decide(GridState state, TimeSpan duration)
    {
        var net = state.NetLoadBeforeStorage.Value;
        Observe(net);

        var hours = duration.TotalHours;
        DischargeThresholdKw = Percentile(DischargePercentile);
        RechargeThresholdKw = Percentile(RechargePercentile);

        var ceiling = _fixedThresholdKw is { } hard ? Math.Min(DischargeThresholdKw, hard) : DischargeThresholdKw;

        if (net > ceiling)
        {
            var wanted = net - ceiling;
            var deliverable = state.StateOfChargeKwh * _legEfficiency / hours;
            var discharge = Math.Min(Math.Min(wanted, state.MaxPowerKw), Math.Max(0, deliverable));
            return discharge <= 0.001 ? StorageSetpoint.Idle : new StorageSetpoint(new Kilowatts(-discharge));
        }

        if (net < RechargeThresholdKw)
        {
            var headroom = RechargeThresholdKw - net;
            var absorbable = (state.CapacityKwh - state.StateOfChargeKwh) / _legEfficiency / hours;
            var charge = Math.Min(Math.Min(headroom, state.MaxPowerKw), Math.Max(0, absorbable));
            return charge <= 0.001 ? StorageSetpoint.Idle : new StorageSetpoint(new Kilowatts(charge));
        }

        return StorageSetpoint.Idle;
    }

    private void Observe(double net)
    {
        _recent.Enqueue(net);
        while (_recent.Count > WindowSize) _recent.Dequeue();
    }

    private double Percentile(double fraction)
    {
        if (_recent.Count == 0) return double.MaxValue;
        var sorted = _recent.ToArray();
        Array.Sort(sorted);
        var index = Math.Clamp((int)Math.Round(fraction * (sorted.Length - 1)), 0, sorted.Length - 1);
        return sorted[index];
    }
}
