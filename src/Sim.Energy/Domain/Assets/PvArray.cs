using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain.Assets;

/// <summary>
/// Rooftop PV. Generation is NEGATIVE power (ADR-002). PV offsets its own house
/// first (A-003) as a consequence of the sign convention: the house meter is the
/// signed sum of its assets, so surplus only becomes an export once the whole
/// neighbourhood is settled.
/// </summary>
public sealed class PvArray(string ownerId, double capacityKwp) : EnergyAssetBase(ownerId, "pv", AssetType.Pv)
{
    public override Kilowatts Measure(MeasurementContext ctx) =>
        new(-capacityKwp * ctx.Environment.IrradianceFactor);
}
