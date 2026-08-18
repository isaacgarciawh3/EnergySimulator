namespace Sim.Energy.Domain;

/// <summary>
/// Entity inside the Neighbourhood aggregate. Invariant: base household
/// consumption is always present, so a house without it cannot be constructed.
/// </summary>
public sealed class House
{
    public House(string id, IEnumerable<Asset> assets)
    {
        Id = id;
        Assets = assets.ToList();
        if (!Assets.Any(a => a.Type == AssetType.BaseLoad))
            throw new ArgumentException($"House {id} must always have base household consumption.", nameof(assets));
    }

    public string Id { get; }
    public IReadOnlyList<Asset> Assets { get; }
}
