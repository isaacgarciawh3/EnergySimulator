namespace Sim.Simulation.Domain;

/// <summary>Owns simulated time. Nothing else in the system decides what time it is.</summary>
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

    public SimulationTick Advance()
    {
        var tick = new SimulationTick(TickIndex, CurrentInstant, TickDuration, _weather.At(CurrentInstant), Seed);
        TickIndex++;
        CurrentInstant += TickDuration;
        return tick;
    }
}
