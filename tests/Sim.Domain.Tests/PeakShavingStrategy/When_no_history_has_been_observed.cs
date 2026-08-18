using Shouldly;
using Sim.Control.Domain;
using static Sim.Domain.Tests.PeakShavingStrategyScenario.StrategyScenario;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>ADR-0010: with no observed history nothing can be called a peak, so the very first decision is idle.</summary>
public class When_no_history_has_been_observed
{
    private readonly StorageSetpoint _firstDecision =
        new PeakShavingStrategy(roundTripEfficiency: RoundTrip)
            .Decide(State(netKw: 500, socKwh: CapacityKwh / 2), Quarter);

    [Fact] public void Should_stay_idle_however_high_the_load() => _firstDecision.ShouldBe(StorageSetpoint.Idle);
}
