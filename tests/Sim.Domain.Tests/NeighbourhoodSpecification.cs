using Shouldly;
using Sim.Application.Configuration;
using Sim.Energy.Domain;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests;

/// <summary>
/// Executable specification of the rules the assignment states as absolute.
/// Written Given/When/Then so each test reads as the requirement it defends.
/// </summary>
public class TheNeighbourhoodSpecification
{
    private static Asset BaseLoad(string house) => new($"{house}/base", house, AssetType.BaseLoad, 0.4);
    private static Asset Pv(string house) => new($"{house}/pv", house, AssetType.Pv, 4.0);
    private static House AHouse(int i) => new($"house-{i:00}", [BaseLoad($"house-{i:00}")]);
    private static List<House> Houses(int count) => Enumerable.Range(1, count).Select(AHouse).ToList();
    private static List<Asset> ChargePoints(int count) => Enumerable.Range(1, count)
        .Select(i => new Asset($"public-charger-{i}/meter", $"public-charger-{i}", AssetType.PublicEvCharger, 11.0))
        .ToList();

    [Fact]
    public void Given_exactly_thirty_houses_and_six_charge_points_When_built_Then_the_neighbourhood_is_valid()
    {
        var neighbourhood = new Neighbourhood(Houses(30), ChargePoints(6));

        neighbourhood.Houses.Count.ShouldBe(30);
        neighbourhood.PublicChargePoints.Count.ShouldBe(6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(31)]
    public void Given_a_house_count_other_than_thirty_When_built_Then_the_neighbourhood_refuses_to_exist(int count)
    {
        var act = () => new Neighbourhood(Houses(count), ChargePoints(6));

        act.ShouldThrow<NeighbourhoodInvariantViolation>()
           .Message.ShouldContain("exactly 30 houses");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(7)]
    public void Given_a_charge_point_count_other_than_six_When_built_Then_the_neighbourhood_refuses_to_exist(int count)
    {
        var act = () => new Neighbourhood(Houses(30), ChargePoints(count));

        act.ShouldThrow<NeighbourhoodInvariantViolation>()
           .Message.ShouldContain("exactly 6 public charge points");
    }

    [Fact]
    public void Given_a_house_without_base_household_consumption_When_built_Then_the_house_refuses_to_exist()
    {
        var act = () => new House("house-01", [Pv("house-01")]);

        act.ShouldThrow<NeighbourhoodInvariantViolation>()
           .Message.ShouldContain("must always have base household consumption");
    }

    [Fact]
    public void Given_a_house_with_two_solar_arrays_When_built_Then_the_house_refuses_to_exist()
    {
        var act = () => new House("house-01", [BaseLoad("house-01"), Pv("house-01"), Pv("house-01")]);

        act.ShouldThrow<NeighbourhoodInvariantViolation>().Message.ShouldContain("at most one");
    }

    [Fact]
    public void Given_a_charge_point_that_is_not_a_public_charger_When_built_Then_the_neighbourhood_refuses_to_exist()
    {
        var wrong = ChargePoints(5).Append(new Asset("x/meter", "x", AssetType.HeatPump, 3.0)).ToList();

        var act = () => new Neighbourhood(Houses(30), wrong);

        act.ShouldThrow<NeighbourhoodInvariantViolation>().Message.ShouldContain("must be of type PublicEvCharger");
    }

    [Fact]
    public void Given_two_assets_sharing_a_meter_id_When_built_Then_the_neighbourhood_refuses_to_exist()
    {
        var houses = Houses(30);
        var clashing = new House("house-31", [new Asset("house-01/base", "house-31", AssetType.BaseLoad, 0.4)]);
        houses[29] = clashing;

        var act = () => new Neighbourhood(houses, ChargePoints(6));

        act.ShouldThrow<NeighbourhoodInvariantViolation>().Message.ShouldContain("uniquely identified");
    }
}

/// <summary>
/// The configuration file supplies the values the world is built from, but it
/// must never be able to talk the neighbourhood out of its invariants.
/// </summary>
public class ConfigurationMustNeverBreakTheInvariants
{
    private static Neighbourhood BuiltFrom(SimulationConfiguration configuration) =>
        NeighbourhoodBuilder.Build(configuration.Validated());

    [Fact]
    public void Given_the_default_configuration_When_the_world_is_built_Then_it_has_thirty_houses_and_six_charge_points()
    {
        var neighbourhood = BuiltFrom(SimulationConfiguration.Default);

        neighbourhood.Houses.Count.ShouldBe(30);
        neighbourhood.PublicChargePoints.Count.ShouldBe(6);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.4)]
    [InlineData(1.0)]
    public void Given_any_asset_share_When_the_world_is_built_Then_the_house_count_is_still_thirty(double share)
    {
        var configuration = SimulationConfiguration.Default with
        {
            PvShare = share, HeatPumpShare = share, HomeEvShare = share,
        };

        BuiltFrom(configuration).Houses.Count.ShouldBe(30);
    }

    [Theory]
    [InlineData(-5.0)]
    [InlineData(99.0)]
    public void Given_an_out_of_range_share_When_validated_Then_it_is_clamped_rather_than_producing_a_broken_world(double share)
    {
        var configuration = (SimulationConfiguration.Default with { PvShare = share }).Validated();

        configuration.PvShare.ShouldBeInRange(0.0, 1.0);
        NeighbourhoodBuilder.Build(configuration).Houses.Count.ShouldBe(30);
    }

    [Fact]
    public void Given_every_house_has_solar_When_the_world_is_built_Then_the_distribution_reports_one_hundred_percent()
    {
        var configuration = SimulationConfiguration.Default with { PvShare = 1.0, HeatPumpShare = 0, HomeEvShare = 0 };

        var distribution = BuiltFrom(configuration).Distribution;

        distribution.PvShare.ShouldBe(1.0);
        distribution.HeatPumpShare.ShouldBe(0.0);
    }

    [Fact]
    public void Given_the_documented_distribution_When_the_world_is_built_Then_the_actual_shares_are_close_to_it()
    {
        // Shares are independent per-house draws, so 30 houses will not land
        // exactly on 40/30/20. The documented figure must still be honest, so we
        // assert it is within a sampling tolerance rather than pretending to be exact.
        var distribution = BuiltFrom(SimulationConfiguration.Default).Distribution;

        distribution.PvShare.ShouldBeInRange(0.20, 0.60);
        distribution.HeatPumpShare.ShouldBeInRange(0.10, 0.50);
        distribution.HomeEvShare.ShouldBeInRange(0.05, 0.40);
    }

    [Fact]
    public void Given_base_household_consumption_is_not_optional_When_the_world_is_built_Then_every_house_has_it()
    {
        var neighbourhood = BuiltFrom(SimulationConfiguration.Default with
        {
            PvShare = 0, HeatPumpShare = 0, HomeEvShare = 0,
        });

        neighbourhood.Houses.ShouldAllBe(h => h.Has(AssetType.BaseLoad));
    }
}

/// <summary>Weather and season must demonstrably drive PV and heat pump behaviour.</summary>
public class WeatherMustInfluenceTheAssets
{
    private static readonly SimulationConfiguration Config = SimulationConfiguration.Default;

    private static double TotalFor(AssetType type, DateTimeOffset start)
    {
        var neighbourhood = NeighbourhoodBuilder.Build(Config with { PvShare = 1.0, HeatPumpShare = 1.0 });
        var simulator = new SimulationRun(neighbourhood, 20260818, start, TimeSpan.FromMinutes(15));
        var ids = neighbourhood.AllAssets.Where(a => a.Type == type).Select(a => a.MeterId).ToHashSet();

        var total = 0.0;
        for (var i = 0; i < 96; i++)
            total += simulator.Advance().Readings.Where(r => ids.Contains(r.MeterId)).Sum(r => r.Power.Value);
        return total;
    }

    [Fact]
    public void Given_a_summer_day_and_a_winter_day_When_simulated_Then_solar_generates_more_in_summer()
    {
        var summer = TotalFor(AssetType.Pv, new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero));
        var winter = TotalFor(AssetType.Pv, new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.Zero));

        // Generation is negative, so "more generation" is a more negative total.
        summer.ShouldBeLessThan(winter);
    }

    [Fact]
    public void Given_a_summer_day_and_a_winter_day_When_simulated_Then_heat_pumps_consume_more_in_winter()
    {
        var summer = TotalFor(AssetType.HeatPump, new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero));
        var winter = TotalFor(AssetType.HeatPump, new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.Zero));

        winter.ShouldBeGreaterThan(summer);
    }

    [Fact]
    public void Given_the_sun_is_down_When_simulated_Then_solar_generates_nothing()
    {
        var neighbourhood = NeighbourhoodBuilder.Build(Config with { PvShare = 1.0 });
        var midnight = new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.Zero);
        var simulator = new SimulationRun(neighbourhood, 20260818, midnight, TimeSpan.FromMinutes(15));
        var pv = neighbourhood.AllAssets.Where(a => a.Type == AssetType.Pv).Select(a => a.MeterId).ToHashSet();

        var readings = simulator.Advance().Readings.Where(r => pv.Contains(r.MeterId));

        readings.ShouldAllBe(r => r.Power.Value == 0);
    }
}
