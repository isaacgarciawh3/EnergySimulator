using Shouldly;
using Sim.Control.Domain;
using static Sim.Domain.Tests.PeakShavingStrategyScenario.StrategyScenario;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>The physical bounds override the policy: an empty battery rests at any peak, a full one rests in any trough.</summary>
public class When_the_battery_has_nothing_to_give_or_take
{
    private readonly bool _emptyRestedAtEveryPeak = true;
    private readonly bool _fullRestedInEveryTrough = true;

    public When_the_battery_has_nothing_to_give_or_take()
    {
        var empty = Warmed(socKwh: 0);
        foreach (var peak in new[] { 100.0, 250.0, 5_000.0 })
            _emptyRestedAtEveryPeak &= empty.Decide(State(peak, 0), Quarter) == StorageSetpoint.Idle;

        var full = Warmed(socKwh: CapacityKwh);
        foreach (var trough in new[] { 0.0, 5.0, -200.0 })
            _fullRestedInEveryTrough &= full.Decide(State(trough, CapacityKwh), Quarter) == StorageSetpoint.Idle;
    }

    [Fact] public void Should_rest_at_any_peak_when_empty() => _emptyRestedAtEveryPeak.ShouldBeTrue();
    [Fact] public void Should_rest_in_any_trough_when_full() => _fullRestedInEveryTrough.ShouldBeTrue();
}
