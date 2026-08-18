using Sim.Energy.Contracts;
using Sim.Energy.Domain.Assets;

namespace Sim.Energy.Domain;

/// <summary>
/// AGGREGATE ROOT of the Energy context. The consistency boundary is the
/// neighbourhood because the "exactly 30 houses / exactly 6 public chargers"
/// invariant spans them all. Houses and charge points are entities inside this
/// boundary, never referenced from outside.
///
/// It measures — it does not account. Turning readings into kWh, imports and
/// exports belongs to the Accounting context (ADR-001).
/// </summary>
public sealed class Neighbourhood
{
    public const int RequiredHouses = 30;
    public const int RequiredPublicChargers = 6;

    private readonly List<IEnergyAsset> _measurementOrder;

    public Neighbourhood(IReadOnlyList<House> houses, IReadOnlyList<PublicEvCharger> publicChargers)
    {
        if (houses.Count != RequiredHouses)
            throw new ArgumentException($"Exactly {RequiredHouses} houses required, got {houses.Count}.", nameof(houses));
        if (publicChargers.Count != RequiredPublicChargers)
            throw new ArgumentException($"Exactly {RequiredPublicChargers} public chargers required, got {publicChargers.Count}.", nameof(publicChargers));

        Houses = houses;
        PublicChargers = publicChargers;
        // Fixed measurement order: floating-point addition is not associative,
        // so a stable order is what keeps aggregate results reproducible.
        _measurementOrder = houses.SelectMany(h => h.Assets).Concat(publicChargers).ToList();
    }

    public IReadOnlyList<House> Houses { get; }
    public IReadOnlyList<PublicEvCharger> PublicChargers { get; }
    public int AssetCount => _measurementOrder.Count;

    public IReadOnlyList<MeterReading> Measure(MeasurementContext ctx)
    {
        var readings = new List<MeterReading>(_measurementOrder.Count);
        foreach (var asset in _measurementOrder)
        {
            var power = asset.Measure(ctx);
            readings.Add(new MeterReading(asset.MeterId, asset.OwnerId, asset.Type, ctx.Instant, power, power.Over(ctx.Duration)));
        }
        return readings;
    }
}
