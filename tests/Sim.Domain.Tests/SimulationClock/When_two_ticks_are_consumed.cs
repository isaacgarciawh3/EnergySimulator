using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationClockScenario;

/// <summary>RF-01 (controllable clock): time moves forward one fixed interval at a time.</summary>
public class When_two_ticks_are_consumed
{
    private readonly SimulationClock _clock;
    private readonly (long Index, DateTimeOffset Instant) _first;
    private readonly (long Index, DateTimeOffset Instant) _second;

    public When_two_ticks_are_consumed()
    {
        _clock = new SimulationClock(Instant, Quarter);
        _first = _clock.NextTick();
        _second = _clock.NextTick();
    }

    [Fact] public void Should_number_the_first_tick_zero() => _first.Index.ShouldBe(0);
    [Fact] public void Should_start_the_first_tick_at_the_start_instant() => _first.Instant.ShouldBe(Instant);
    [Fact] public void Should_number_the_second_tick_one() => _second.Index.ShouldBe(1);
    [Fact] public void Should_start_the_second_tick_exactly_one_interval_later() => _second.Instant.ShouldBe(Instant + Quarter);
    [Fact] public void Should_now_point_past_both_consumed_ticks() => _clock.CurrentInstant.ShouldBe(Instant + Quarter + Quarter);
    [Fact] public void Should_never_change_the_interval_length() => _clock.TickDuration.ShouldBe(Quarter);
    [Fact] public void Should_have_counted_two_ticks() => _clock.TickIndex.ShouldBe(2);
}
