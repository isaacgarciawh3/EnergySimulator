namespace Sim.Simulation.Domain;

/// <summary>
/// Leaf of the <see cref="SimulationRun"/> aggregate: owns simulated time and
/// nothing else. Time only moves forward, one fixed-length interval at a time.
/// </summary>
public sealed class SimulationClock
{
    public SimulationClock(DateTimeOffset start, TimeSpan tickDuration)
    {
        if (tickDuration <= TimeSpan.Zero)
            throw new SimulationInvariantViolation("SimulationClock.TickDuration must be positive.");
        CurrentInstant = start;
        TickDuration = tickDuration;
    }

    public DateTimeOffset CurrentInstant { get; private set; }
    public TimeSpan TickDuration { get; }
    public long TickIndex { get; private set; }

    public (long Index, DateTimeOffset Instant) NextTick()
    {
        var tick = (TickIndex, CurrentInstant);
        TickIndex++;
        CurrentInstant += TickDuration;
        return tick;
    }
}
