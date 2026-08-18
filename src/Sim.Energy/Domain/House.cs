namespace Sim.Energy.Domain;

/// <summary>
/// Entity inside the Neighbourhood aggregate. A house is a set of metered assets
/// and always includes base household consumption - a house without it is not
/// representable, because the constructor refuses to build one.
/// </summary>
public sealed class House
{
    public House(string id, IEnumerable<Asset> assets)
    {
        Id = id;
        Assets = assets.ToList();

        NeighbourhoodInvariants.EveryHouseMustHaveBaseHouseholdConsumption(id, Assets);
        NeighbourhoodInvariants.EveryHouseMustHaveAtMostOneOfEachAssetType(id, Assets);
    }

    public string Id { get; }
    public IReadOnlyList<Asset> Assets { get; }

    public bool Has(AssetType type) => Assets.Any(a => a.Type == type);
}
