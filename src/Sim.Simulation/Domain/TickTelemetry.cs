using Sim.SharedKernel;

namespace Sim.Simulation.Domain;

/// <summary>
/// Everything one advance of the simulation produced. This is the run's whole
/// answer to its caller - there is nothing to ask the run afterwards that is
/// not already in here.
/// </summary>
public sealed record TickTelemetry(
    long TickIndex,
    DateTimeOffset Instant,
    TimeSpan Duration,
    WeatherConditions Weather,
    IReadOnlyList<PowerReading> Readings,
    IReadOnlyCollection<string> OccupiedChargePoints);
