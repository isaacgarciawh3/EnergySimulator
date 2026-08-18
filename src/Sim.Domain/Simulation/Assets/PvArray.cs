using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation.Assets;

/// <summary>
/// Rooftop PV: generation = installed capacity × the tick's irradiance factor.
/// Negative sign = generation (ADR-002). PV offsets its own house first (A-003)
/// simply because the house meter is the signed sum of its asset readings.
/// </summary>
public sealed class PvArray(string ownerId, double capacityKwp) : EnergyAssetBase(ownerId, "pv", AssetType.Pv)
{
    public override Kilowatts Measure(TickContext ctx) =>
        new(-capacityKwp * ctx.Weather.IrradianceFactor);
}
