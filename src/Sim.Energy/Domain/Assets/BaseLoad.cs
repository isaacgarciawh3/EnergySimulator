using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain.Assets;

/// <summary>Always-present household consumption (A-008): per-house baseline shaped by a morning/evening curve, with deterministic jitter.</summary>
public sealed class BaseLoad(string ownerId, double baselineKw) : EnergyAssetBase(ownerId, "base", AssetType.BaseLoad)
{
    public override Kilowatts Measure(MeasurementContext ctx) =>
        new(baselineKw * DailyShape(ctx.Instant.TimeOfDay.TotalHours) * (0.9 + 0.2 * Noise(ctx)));

    private static double DailyShape(double hour) => hour switch
    {
        < 6 => 0.55,   // night trough
        < 9 => 1.5,    // morning peak
        < 17 => 0.9,   // daytime
        < 22 => 1.8,   // evening peak
        _ => 0.8,
    };
}
