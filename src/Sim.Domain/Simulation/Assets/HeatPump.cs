using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation.Assets;

/// <summary>
/// Balance-point linear model (A-005): electrical demand grows linearly as the
/// outdoor temperature drops below 15 °C, capped at rated power. COP is folded
/// into the per-degree coefficient — we model electrical draw directly.
/// </summary>
public sealed class HeatPump(string ownerId, double kwPerDegree, double maxKw)
    : EnergyAssetBase(ownerId, "heat-pump", AssetType.HeatPump)
{
    public const double BalancePointC = 15.0;

    public override Kilowatts Measure(TickContext ctx)
    {
        var demand = Math.Min(maxKw, kwPerDegree * Math.Max(0.0, BalancePointC - ctx.Weather.TemperatureC));
        var jitter = 0.95 + 0.1 * Noise(ctx);
        return new Kilowatts(demand * jitter);
    }
}
