using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests;

/// <summary>
/// The Energy context describes the physical world, and the assignment fixes
/// that world at thirty houses and six shared charge points. These tests assert
/// that an invalid world cannot be constructed at all - the invariant is
/// enforced in the constructor, not checked later by someone who remembers to.
/// </summary>
public sealed class DomainInvariantTests
{
    // 9
    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(31)]
    [InlineData(60)]
    public void A_neighbourhood_rejects_any_house_count_other_than_thirty(int houseCount) =>
        Should.Throw<NeighbourhoodInvariantViolation>(() => new Neighbourhood(Houses(houseCount), ChargePoints(6)));

    // 9
    [Fact]
    public void A_neighbourhood_of_exactly_thirty_houses_is_accepted()
    {
        var neighbourhood = new Neighbourhood(Houses(Neighbourhood.RequiredHouses), ChargePoints(6));

        neighbourhood.Houses.Count.ShouldBe(30);
        Neighbourhood.RequiredHouses.ShouldBe(30);
    }

    // 10
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(12)]
    public void A_neighbourhood_rejects_any_public_charger_count_other_than_six(int chargerCount) =>
        Should.Throw<NeighbourhoodInvariantViolation>(() => new Neighbourhood(Houses(30), ChargePoints(chargerCount)));

    // 10
    [Fact]
    public void A_neighbourhood_of_exactly_six_public_chargers_is_accepted()
    {
        var neighbourhood = new Neighbourhood(Houses(30), ChargePoints(Neighbourhood.RequiredPublicChargers));

        neighbourhood.PublicChargePoints.Count.ShouldBe(6);
        Neighbourhood.RequiredPublicChargers.ShouldBe(6);
    }

    // 10
    [Fact]
    public void A_public_charge_point_must_actually_be_a_public_charger()
    {
        var impostors = ChargePoints(5)
            .Append(new Asset("house-01/pv", "house-01", AssetType.Pv, 4.0))
            .ToList();

        Should.Throw<NeighbourhoodInvariantViolation>(() => new Neighbourhood(Houses(30), impostors));
    }

    // 11
    [Fact]
    public void A_house_without_base_load_cannot_be_constructed()
    {
        Asset[] solarOnly = [new Asset("house-01/pv", "house-01", AssetType.Pv, 4.0)];

        Should.Throw<NeighbourhoodInvariantViolation>(() => new House("house-01", solarOnly));
        Should.Throw<NeighbourhoodInvariantViolation>(() => new House("house-01", []));
    }

    // 11
    [Fact]
    public void A_house_with_base_load_is_constructed_and_keeps_its_assets()
    {
        Asset[] assets =
        [
            new Asset("house-01/base", "house-01", AssetType.BaseLoad, 0.4),
            new Asset("house-01/pv", "house-01", AssetType.Pv, 4.0),
        ];

        var house = new House("house-01", assets);

        house.Id.ShouldBe("house-01");
        house.Assets.Count.ShouldBe(2);
        house.Assets.ShouldContain(a => a.Type == AssetType.BaseLoad);
    }

    // 11
    [Fact]
    public void Every_asset_of_every_house_and_charger_appears_exactly_once_in_all_assets()
    {
        var neighbourhood = new Neighbourhood(Houses(30), ChargePoints(6));

        neighbourhood.AllAssets.Count.ShouldBe(36);
        neighbourhood.AllAssets.Select(a => a.MeterId).Distinct().Count().ShouldBe(36);
        // The battery is commanded, not simulated from the weather, so it is deliberately excluded.
        neighbourhood.AllAssets.ShouldNotContain(a => a.MeterId == "neighbourhood/battery");
    }
}
