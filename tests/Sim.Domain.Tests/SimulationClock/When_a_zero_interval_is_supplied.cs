using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationClockScenario;

/// <summary>RF-01: a clock that cannot move must be unrepresentable.</summary>
public class When_a_zero_interval_is_supplied
{
    private readonly Exception? _refusal = Record.Exception(() => new SimulationClock(Instant, TimeSpan.Zero));

    [Fact] public void Should_refuse_to_exist() => _refusal.ShouldBeOfType<SimulationInvariantViolation>();
}
