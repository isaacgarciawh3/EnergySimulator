using Sim.Energy.Contracts;

namespace Sim.Energy.Domain;

/// <summary>
/// Entity inside the Neighbourhood aggregate. Invariant: base household
/// consumption is always present — a house without it is not representable.
/// </summary>
public sealed class House
{
    public House(string id, IEnumerable<IEnergyAsset> assets)
    {
        Id = id;
        Assets = assets.ToList();
        if (!Assets.Any(a => a.Type == AssetType.BaseLoad))
            throw new ArgumentException($"House {id} must always have base household consumption.", nameof(assets));
    }

    public string Id { get; }
    public IReadOnlyList<IEnergyAsset> Assets { get; }
}
