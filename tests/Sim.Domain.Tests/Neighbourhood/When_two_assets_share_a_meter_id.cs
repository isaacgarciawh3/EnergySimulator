using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>Every meter is an identity - the books post by meter id, so a duplicate would silently merge two assets' energy.</summary>
public class When_two_assets_share_a_meter_id
{
    private readonly Exception? _refusal;

    public When_two_assets_share_a_meter_id()
    {
        var houses = Houses(30).ToList();
        houses[29] = new House("house-31", [new Asset("house-01/base", "house-31", AssetType.BaseLoad, 0.4)]);
        _refusal = Record.Exception(() => new Sim.Energy.Domain.Neighbourhood(houses, ChargePoints(6)));
    }

    [Fact] public void Should_refuse_the_duplicate() => _refusal.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_explain_that_meters_identify() => _refusal!.Message.ShouldContain("uniquely identified");
}
