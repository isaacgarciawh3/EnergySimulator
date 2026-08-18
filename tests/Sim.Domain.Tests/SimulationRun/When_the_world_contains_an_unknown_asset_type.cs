using Shouldly;
using Sim.Simulation.Domain;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>
/// RF-03 (assets are extensible): a type nobody wrote a behaviour for fails at
/// construction, never silently mid-simulation.
/// </summary>
public class When_the_world_contains_an_unknown_asset_type
{
    private readonly Exception? _refusal;

    public When_the_world_contains_an_unknown_asset_type()
    {
        var houses = Houses(30).ToList();
        houses[0] = new House("house-01",
        [
            new Asset("house-01/base", "house-01", AssetType.BaseLoad, 0.4),
            new Asset("house-01/mystery", "house-01", (AssetType)999, 1.0),
        ]);
        _refusal = Record.Exception(() => RunOf(new Neighbourhood(houses, ChargePoints(6))));
    }

    [Fact] public void Should_refuse_to_build_the_run() => _refusal.ShouldBeOfType<SimulationInvariantViolation>();
}
