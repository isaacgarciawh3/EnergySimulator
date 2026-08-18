namespace Sim.Energy.Domain;

/// <summary>
/// Entity inside the Neighbourhood aggregate: a set of metered assets. Base
/// household consumption is an invariant, not an option, and no house carries
/// two assets of the same kind - a house that would is not representable.
/// </summary>
public sealed class House
{
    public House(string id, IEnumerable<Asset> assets)
    {
        Id = id;
        Assets = assets.ToList();

        RefuseUnlessBaseConsumptionIsPresent();
        RefuseUnlessEachAssetKindAppearsOnce();
    }

    private void RefuseUnlessBaseConsumptionIsPresent()
    {
        if (!Assets.Any(a => a.Type == AssetType.BaseLoad))
            throw new NeighbourhoodInvariantViolation(
                $"House '{Id}' must always have base household consumption, but no {AssetType.BaseLoad} asset was supplied.");
    }

    private void RefuseUnlessEachAssetKindAppearsOnce()
    {
        var duplicate = Assets.GroupBy(a => a.Type).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new NeighbourhoodInvariantViolation(
                $"House '{Id}' has {duplicate.Count()} assets of type {duplicate.Key}, but at most one is allowed.");
    }

    public string Id { get; }
    public IReadOnlyList<Asset> Assets { get; }

    public bool Has(AssetType type) => Assets.Any(a => a.Type == type);
}
