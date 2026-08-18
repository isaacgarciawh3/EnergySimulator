namespace Sim.Domain.Simulation;

/// <summary>The controllable simulation clock. Owns simulated time; nothing else does.</summary>
public sealed class SimulationClock
{
    public SimulationClock(DateTimeOffset start, TimeSpan tickDuration)
    {
        if (tickDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be positive.");
        CurrentInstant = start;
        TickDuration = tickDuration;
    }

    public DateTimeOffset CurrentInstant { get; private set; }
    public TimeSpan TickDuration { get; }
    public long TickIndex { get; private set; }

    public TickContext NextContext(WeatherSample weather, ulong seed)
    {
        var ctx = new TickContext(TickIndex, CurrentInstant, TickDuration, weather, seed);
        TickIndex++;
        CurrentInstant += TickDuration;
        return ctx;
    }
}
