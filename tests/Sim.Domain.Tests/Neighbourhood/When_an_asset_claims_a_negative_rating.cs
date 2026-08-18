using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>
/// Ratings are magnitudes; the SIGN of a reading carries direction (ADR-0002),
/// so a negative nameplate would double-negate generation. The audit showed
/// this invariant had NEVER been tested - this is its first scenario.
/// </summary>
public class When_an_asset_claims_a_negative_rating
{
    private readonly Exception? _refusal;

    public When_an_asset_claims_a_negative_rating()
    {
        var houses = Houses(30).ToList();
        houses[0] = new House("house-01", [new Asset("house-01/base", "house-01", AssetType.BaseLoad, -0.4)]);
        _refusal = Record.Exception(() => new Sim.Energy.Domain.Neighbourhood(houses, ChargePoints(6)));
    }

    [Fact] public void Should_refuse_the_negative_nameplate() => _refusal.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_explain_that_ratings_are_magnitudes() => _refusal!.Message.ShouldContain("magnitudes");
}
