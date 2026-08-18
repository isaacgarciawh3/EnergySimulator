using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation.Assets;

public abstract class EnergyAssetBase(string ownerId, string meterSuffix, AssetType type) : IEnergyAsset
{
    public string OwnerId { get; } = ownerId;
    public string MeterId { get; } = $"{ownerId}/{meterSuffix}";
    public AssetType Type { get; } = type;

    /// <summary>Per-asset noise stream derived from the meter id (FNV-1a).</summary>
    protected ulong Stream { get; } = Fnv1a($"{ownerId}/{meterSuffix}");

    public abstract Kilowatts Measure(TickContext ctx);

    protected double Noise(TickContext ctx, ulong salt = 0) =>
        DeterministicNoise.Sample(ctx.Seed, Stream ^ salt, ctx.TickIndex);

    protected double DailyNoise(TickContext ctx, ulong salt, long day) =>
        DeterministicNoise.Sample(ctx.Seed, Stream ^ salt, day);

    private static ulong Fnv1a(string s)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in s) { hash ^= c; hash *= 1099511628211UL; }
        return hash;
    }
}
