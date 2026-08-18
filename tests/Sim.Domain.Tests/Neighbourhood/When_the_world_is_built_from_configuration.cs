using Shouldly;
using Sim.Application.Configuration;
using Sim.Energy.Domain;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>
/// R-24/A-013: configuration supplies the values the world is built from, but
/// it can never talk the aggregate out of its invariants - hostile shares are
/// clamped, and the counts hold at every setting.
/// </summary>
public class When_the_world_is_built_from_configuration
{
    private readonly Sim.Energy.Domain.Neighbourhood _fromDefaults;
    private readonly Sim.Energy.Domain.Neighbourhood _fromHostileShares;
    private readonly SimulationConfiguration _clamped;
    private readonly Sim.Energy.Domain.Neighbourhood _allSolar;

    public When_the_world_is_built_from_configuration()
    {
        _fromDefaults = NeighbourhoodBuilder.Build(SimulationConfiguration.Default);
        _clamped = (SimulationConfiguration.Default with { PvShare = 99, HeatPumpShare = -5, HomeEvShare = 42 }).Validated();
        _fromHostileShares = NeighbourhoodBuilder.Build(_clamped);
        _allSolar = NeighbourhoodBuilder.Build(SimulationConfiguration.Default with { PvShare = 1.0, HeatPumpShare = 0, HomeEvShare = 0 });
    }

    [Fact] public void Should_build_thirty_houses_from_the_defaults() => _fromDefaults.Houses.Count.ShouldBe(30);
    [Fact] public void Should_build_six_charge_points_from_the_defaults() => _fromDefaults.PublicChargePoints.Count.ShouldBe(6);
    [Fact] public void Should_clamp_hostile_shares_into_fractions() => _clamped.PvShare.ShouldBeInRange(0.0, 1.0);
    [Fact] public void Should_still_build_thirty_houses_from_hostile_shares() => _fromHostileShares.Houses.Count.ShouldBe(30);
    [Fact] public void Should_give_every_house_solar_when_the_share_is_total() => _allSolar.Distribution.PvShare.ShouldBe(1.0);
    [Fact] public void Should_never_deal_a_heat_pump_at_share_zero() => _allSolar.Distribution.HeatPumpShare.ShouldBe(0.0);
    [Fact] public void Should_keep_base_consumption_in_every_house() => _fromDefaults.Houses.ShouldAllBe(h => h.Has(AssetType.BaseLoad));
    [Fact] public void Should_land_the_documented_distribution_near_its_shares() => _fromDefaults.Distribution.PvShare.ShouldBeInRange(0.20, 0.60);
}
