using Shouldly;
using Sim.Application.Configuration;
using Sim.Energy.Domain;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests;

/// <summary>
/// Layout determinism of the WORLD BUILDER (application layer): the same
/// configuration always builds the same neighbourhood. The run's own
/// determinism scenarios live in SimulationRun/, converted to the scenario
/// standard (ADR-0014); these will follow when the application layer is next
/// altered.
/// </summary>
public sealed class DeterminismTests
{
    private static string LayoutOf(Neighbourhood neighbourhood) =>
        string.Join('\n', neighbourhood.AllAssets.Select(a =>
            $"{a.MeterId}|{a.OwnerId}|{a.Type}|{a.RatedPowerKw:R}|{a.ResponseCoefficient:R}"));



    // 7
    [Fact]
    public void The_same_configuration_produces_the_same_asset_layout()
    {
        var configuration = SimulationConfiguration.Default;

        var first = NeighbourhoodBuilder.Build(configuration);
        var second = NeighbourhoodBuilder.Build(configuration);

        LayoutOf(first).ShouldBe(LayoutOf(second));
        first.AllAssets.ShouldBe(second.AllAssets);           // Asset is a record: structural equality
        first.Houses.Count.ShouldBe(second.Houses.Count);
        first.Battery.ShouldBe(second.Battery);
    }

    // 8
    [Fact]
    public void A_different_seed_produces_a_different_asset_layout()
    {
        var configuration = SimulationConfiguration.Default;

        var first = NeighbourhoodBuilder.Build(configuration with { Seed = 1 });
        var second = NeighbourhoodBuilder.Build(configuration with { Seed = 2 });

        // Guards against the layout being a constant that ignores the seed entirely.
        LayoutOf(first).ShouldNotBe(LayoutOf(second));
    }

}
