namespace Sim.Simulation.Domain;

/// <summary>Everything a behaviour may consult to decide what an asset is doing.</summary>
public sealed record SimulationTick(
    long TickIndex,
    DateTimeOffset Instant,
    TimeSpan Duration,
    WeatherConditions Weather,
    ulong Seed);
