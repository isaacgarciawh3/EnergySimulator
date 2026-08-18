namespace Sim.Simulation.Contracts;

/// <summary>
/// Published language of the Simulation context: "this is the world at tick N".
/// It is the ONLY thing that leaves this context. No other context may reach
/// into <c>Sim.Simulation.Domain</c>.
/// </summary>
public sealed record TickEnvironment(
    long TickIndex,
    DateTimeOffset Instant,
    TimeSpan Duration,
    double TemperatureC,
    double CloudCover,
    double IrradianceFactor,
    string Season);
