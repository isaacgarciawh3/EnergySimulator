using Shouldly;
using Sim.Energy.Domain;

namespace Sim.Domain.Tests.HouseScenario;

/// <summary>R-05: base household consumption is ALWAYS present - a house without it is not representable.</summary>
public class When_a_house_is_assembled_without_base_consumption
{
    private readonly Exception? _solarOnly =
        Record.Exception(() => new House("house-01", [new Asset("house-01/pv", "house-01", AssetType.Pv, 4.0)]));

    private readonly Exception? _empty =
        Record.Exception(() => new House("house-01", []));

    [Fact] public void Should_refuse_a_solar_only_house() => _solarOnly.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_refuse_an_empty_house() => _empty.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_name_the_missing_consumption() => _empty!.Message.ShouldContain("base household consumption");
}
