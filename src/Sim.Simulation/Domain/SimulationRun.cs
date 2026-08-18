using Sim.Simulation.Contracts;

namespace Sim.Simulation.Domain;

/// <summary>
/// AGGREGATE ROOT of the Simulation context. Owns simulated time and the
/// environment — nothing else in the system is allowed to know what time it is.
/// It knows nothing about houses, assets or kilowatt-hours.
/// </summary>
public sealed class SimulationRun
{
    private readonly WeatherModel _weather;

    public SimulationRun(ulong seed, DateTimeOffset start, TimeSpan tickDuration)
    {
        if (tickDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be positive.");
        Seed = seed;
        StartedAt = start;
        CurrentInstant = start;
        TickDuration = tickDuration;
        _weather = new WeatherModel(seed);
    }

    public ulong Seed { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset CurrentInstant { get; private set; }
    public TimeSpan TickDuration { get; }
    public long TickIndex { get; private set; }

    /// <summary>Advances one tick and publishes the environment for that tick.</summary>
    public TickEnvironment Advance()
    {
        var w = _weather.At(CurrentInstant);
        var env = new TickEnvironment(TickIndex, CurrentInstant, TickDuration,
            w.TemperatureC, w.CloudCover, w.IrradianceFactor, w.Season.ToString());
        TickIndex++;
        CurrentInstant += TickDuration;
        return env;
    }
}
