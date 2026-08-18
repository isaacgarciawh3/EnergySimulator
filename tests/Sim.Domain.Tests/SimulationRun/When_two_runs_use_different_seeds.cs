using Shouldly;
using Sim.Application.Configuration;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>Guards against the seed being decorative: different seeds must produce different worlds in motion.</summary>
public class When_two_runs_use_different_seeds
{
    private readonly bool _identical;

    public When_two_runs_use_different_seeds()
    {
        var configuration = SimulationConfiguration.Default;
        var first = RunFrom(configuration with { Seed = 1 }).Advance().Readings;
        var second = RunFrom(configuration with { Seed = 2 }).Advance().Readings;

        _identical = first.Count == second.Count
            && first.Zip(second).All(p => p.First.MeterId == p.Second.MeterId
                                       && p.First.Power.Value.Equals(p.Second.Power.Value));
    }

    private static SimulationRun RunFrom(SimulationConfiguration configuration) =>
        new(NeighbourhoodBuilder.Build(configuration), unchecked((ulong)configuration.Seed),
            configuration.StartInstant, configuration.TickDuration);

    [Fact] public void Should_produce_different_readings() => _identical.ShouldBeFalse();
}
