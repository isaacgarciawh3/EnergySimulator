using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation;

/// <summary>Everything an asset may consult when measuring one tick.</summary>
public sealed record TickContext(
    long TickIndex,
    DateTimeOffset Instant,
    TimeSpan Duration,
    WeatherSample Weather,
    ulong Seed);

/// <summary>
/// The single call signature every asset answers (Strategy pattern). Signed
/// power: consumption positive, generation negative (ADR-002). Assets may keep
/// internal state (EV sessions); the tick loop is therefore strictly sequential.
/// </summary>
public interface IEnergyAsset
{
    string MeterId { get; }
    string OwnerId { get; }
    AssetType Type { get; }
    Kilowatts Measure(TickContext ctx);
}
