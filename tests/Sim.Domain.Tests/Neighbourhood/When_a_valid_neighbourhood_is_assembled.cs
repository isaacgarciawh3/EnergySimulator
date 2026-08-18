using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>R-20/R-21: exactly thirty houses and six public charge points make a valid world, and it can describe itself.</summary>
public class When_a_valid_neighbourhood_is_assembled
{
    private readonly Sim.Energy.Domain.Neighbourhood _world = new(Houses(30), ChargePoints(6));

    [Fact] public void Should_hold_exactly_thirty_houses() => _world.Houses.Count.ShouldBe(30);
    [Fact] public void Should_hold_exactly_six_charge_points() => _world.PublicChargePoints.Count.ShouldBe(6);
    [Fact] public void Should_enumerate_every_meter_in_a_fixed_order() => _world.AllAssets.Count.ShouldBe(36);
    [Fact] public void Should_answer_what_sits_behind_a_meter() => _world.TypeOf("house-01/base").ShouldBe(AssetType.BaseLoad);
    [Fact] public void Should_state_its_distribution_as_prose() => _world.Distribution.ToString().ShouldContain("30 houses");
    [Fact] public void Should_have_no_battery_unless_one_is_installed() => _world.Battery.ShouldBeNull();
}
