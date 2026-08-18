using Shouldly;
using Sim.Energy.Domain;

namespace Sim.Domain.Tests.HouseScenario;

/// <summary>One asset of each kind per house - a second solar array is a modelling error, refused at birth.</summary>
public class When_a_house_carries_two_of_the_same_asset
{
    private readonly Exception? _refusal = Record.Exception(() => new House("house-01",
    [
        new Asset("house-01/base", "house-01", AssetType.BaseLoad, 0.4),
        new Asset("house-01/pv-a", "house-01", AssetType.Pv, 4.0),
        new Asset("house-01/pv-b", "house-01", AssetType.Pv, 4.0),
    ]));

    [Fact] public void Should_refuse_the_second_array() => _refusal.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_allow_at_most_one_of_each_kind() => _refusal!.Message.ShouldContain("at most one");
}
