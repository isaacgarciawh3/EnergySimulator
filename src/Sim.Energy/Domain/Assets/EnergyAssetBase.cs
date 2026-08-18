using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain.Assets;

public abstract class EnergyAssetBase(string ownerId, string meterSuffix, AssetType type) : IEnergyAsset
{
    public string OwnerId { get; } = ownerId;
    public string MeterId { get; } = $"{ownerId}/{meterSuffix}";
    public AssetType Type { get; } = type;

    private readonly ulong _stream = DeterministicNoise.StreamOf($"{ownerId}/{meterSuffix}");

    public abstract Kilowatts Measure(MeasurementContext ctx);

    protected double Noise(MeasurementContext ctx, ulong salt = 0) =>
        DeterministicNoise.Sample(ctx.Seed, _stream ^ salt, ctx.TickIndex);

    protected double PerDayNoise(MeasurementContext ctx, ulong salt, long day) =>
        DeterministicNoise.Sample(ctx.Seed, _stream ^ salt, day);
}
