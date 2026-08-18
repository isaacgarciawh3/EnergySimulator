using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation.Assets;

/// <summary>
/// Always-present household consumption (A-008): a per-house baseline shaped by
/// a morning/evening daily curve with ±10% deterministic jitter.
/// </summary>
public sealed class BaseLoad(string ownerId, double baselineKw) : EnergyAssetBase(ownerId, "base", AssetType.BaseLoad)
{
    public override Kilowatts Measure(TickContext ctx)
    {
        var shape = DailyShape(ctx.Instant.TimeOfDay.TotalHours);
        var jitter = 0.9 + 0.2 * Noise(ctx);
        return new Kilowatts(baselineKw * shape * jitter);
    }

    private static double DailyShape(double hour) => hour switch
    {
        < 6 => 0.55,   // night trough
        < 9 => 1.5,    // morning peak
        < 17 => 0.9,   // daytime
        < 22 => 1.8,   // evening peak
        _ => 0.8,
    };
}
