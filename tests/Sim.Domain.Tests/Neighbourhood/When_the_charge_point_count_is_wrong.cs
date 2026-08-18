using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>R-21: EXACTLY six public charge points, same law.</summary>
public class When_the_charge_point_count_is_wrong
{
    private static Exception? Refusal(int count) =>
        Record.Exception(() => new Sim.Energy.Domain.Neighbourhood(Houses(30), ChargePoints(count)));

    private readonly Exception? _none = Refusal(0);
    private readonly Exception? _oneShort = Refusal(5);
    private readonly Exception? _oneExtra = Refusal(7);

    [Fact] public void Should_refuse_none_at_all() => _none.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_refuse_five() => _oneShort.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_refuse_seven() => _oneExtra.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_name_the_rule_it_broke() => _oneShort!.Message.ShouldContain("exactly 6 public charge points");
}
