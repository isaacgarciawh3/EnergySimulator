using Shouldly;
using Sim.Energy.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.NeighbourhoodScenario;

/// <summary>R-20: EXACTLY thirty - a constraint of the assignment, not a setting. Too few, one short, one extra: all refused at birth.</summary>
public class When_the_house_count_is_wrong
{
    private static Exception? Refusal(int count) =>
        Record.Exception(() => new Sim.Energy.Domain.Neighbourhood(Houses(count), ChargePoints(6)));

    private readonly Exception? _none = Refusal(0);
    private readonly Exception? _oneShort = Refusal(29);
    private readonly Exception? _oneExtra = Refusal(31);

    [Fact] public void Should_refuse_an_empty_neighbourhood() => _none.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_refuse_twenty_nine() => _oneShort.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_refuse_thirty_one() => _oneExtra.ShouldBeOfType<NeighbourhoodInvariantViolation>();
    [Fact] public void Should_name_the_rule_it_broke() => _oneShort!.Message.ShouldContain("exactly 30 houses");
}
