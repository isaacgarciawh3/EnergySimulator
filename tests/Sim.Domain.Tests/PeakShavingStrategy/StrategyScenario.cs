using Sim.Control.Domain;
using Sim.SharedKernel;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>Shared vocabulary: a plausible winter day for thirty houses, and a warmed strategy that has observed it.</summary>
internal static class StrategyScenario
{
    public const double CapacityKwh = 250;
    public const double MaxPowerKw = 80;
    public const double RoundTrip = 0.9;

    public static readonly double LegEfficiency = Math.Sqrt(RoundTrip);
    public static readonly TimeSpan Quarter = TimeSpan.FromMinutes(15);
    public static readonly double QuarterHours = Quarter.TotalHours;

    public static IEnumerable<double> DailyLoadKw(int days = 1)
    {
        for (var day = 0; day < days; day++)
            for (var slot = 0; slot < 96; slot++)
            {
                var hour = slot / 4.0;
                var shape = hour switch { < 6 => 0.55, < 9 => 1.50, < 17 => 0.90, < 22 => 1.80, _ => 0.80 };
                var wobble = 1.0 + 0.08 * Math.Sin(2 * Math.PI * (slot + 13 * day) / 17.0);
                yield return 35.0 * shape * wobble;
            }
    }

    public static GridState State(double netKw, double socKwh) =>
        new(new Kilowatts(netKw), socKwh, CapacityKwh, MaxPowerKw);

    public static PeakShavingStrategy Warmed(double socKwh = CapacityKwh / 2, double? fixedThresholdKw = null)
    {
        var strategy = new PeakShavingStrategy(fixedThresholdKw, RoundTrip);
        foreach (var load in DailyLoadKw()) strategy.Decide(State(load, socKwh), Quarter);
        return strategy;
    }
}
