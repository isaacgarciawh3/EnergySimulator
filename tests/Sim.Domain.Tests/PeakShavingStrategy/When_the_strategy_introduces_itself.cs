using Shouldly;
using Sim.Control.Domain;
using static Sim.Domain.Tests.PeakShavingStrategyScenario.StrategyScenario;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>The dashboard prints the strategy's name; it must state the policy, and the hard ceiling only when one exists.</summary>
public class When_the_strategy_introduces_itself
{
    private readonly string _withoutACeiling = new PeakShavingStrategy(roundTripEfficiency: RoundTrip).Name;
    private readonly string _withACeiling = new PeakShavingStrategy(fixedThresholdKw: 45, roundTripEfficiency: RoundTrip).Name;

    [Fact] public void Should_state_the_percentile_policy() => _withoutACeiling.ShouldBe("Peak shaving: top 20% of load");
    [Fact] public void Should_state_the_hard_ceiling_when_one_exists() => _withACeiling.ShouldBe("Peak shaving: top 20% of load, hard ceiling 45 kW");
}
