using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain.Assets;

/// <summary>
/// Balance-point model (A-005): electrical draw rises linearly as outdoor
/// temperature falls below 15 C, capped at rated power. COP is folded into the
/// per-degree coefficient — we model electrical draw directly, not heat output.
/// </summary>
public sealed class HeatPump(string ownerId, double kwPerDegree, double maxKw)
    : EnergyAssetBase(ownerId, "heat-pump", AssetType.HeatPump)
{
    public const double BalancePointC = 15.0;

    public override Kilowatts Measure(MeasurementContext ctx)
    {
        var deficit = Math.Max(0.0, BalancePointC - ctx.Environment.TemperatureC);
        var demand = Math.Min(maxKw, kwPerDegree * deficit);
        return new Kilowatts(demand * (0.95 + 0.1 * Noise(ctx)));
    }
}
