using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationClockScenario;

/// <summary>RF-01: time cannot be asked to run backwards.</summary>
public class When_a_negative_interval_is_supplied
{
    private readonly Exception? _refusal = Record.Exception(() => new SimulationClock(Instant, TimeSpan.FromMinutes(-15)));

    [Fact] public void Should_refuse_to_exist() => _refusal.ShouldBeOfType<ArgumentOutOfRangeException>();
}
