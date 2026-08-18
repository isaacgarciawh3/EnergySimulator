using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>The six shared points are chargers by definition - a heat pump wearing the badge is refused by name.</summary>
public class When_an_impostor_joins_the_charge_points
{
    private readonly Exception? _refusal;

    public When_an_impostor_joins_the_charge_points()
    {
        var points = ChargePoints(5).Append(new Asset("impostor/meter", "impostor", AssetType.HeatPump, 3.0)).ToList();
        _refusal = Record.Exception(() => new Sim.Energy.Domain.Neighbourhood(Houses(30), points));
    }

    [Fact] public void Should_refuse_the_impostor() => _refusal.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_name_the_offending_meter() => _refusal!.Message.ShouldContain("impostor/meter");
}
