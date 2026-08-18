using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation;

/// <summary>A house composes 1..n assets; base load is an invariant, not an option.</summary>
public sealed class House
{
    public House(string id, IEnumerable<IEnergyAsset> assets)
    {
        Id = id;
        Assets = assets.ToList();
        if (Assets.All(a => a.Type != AssetType.BaseLoad))
            throw new ArgumentException($"House {id} must always have base household consumption.", nameof(assets));
    }

    public string Id { get; }
    public IReadOnlyList<IEnergyAsset> Assets { get; }
}
