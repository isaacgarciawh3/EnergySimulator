using Shouldly;
using Sim.Control.Domain;
using static Sim.Domain.Tests.PeakShavingStrategyScenario.StrategyScenario;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>R-46/R-47: above the observed top the battery discharges, below the observed bottom it recharges, in between it rests.</summary>
public class When_the_load_leaves_the_observed_band
{
    private readonly PeakShavingStrategy _strategy = Warmed();
    private readonly StorageSetpoint _atASpike;
    private readonly StorageSetpoint _inATrough;
    private readonly StorageSetpoint _inTheMiddle;

    public When_the_load_leaves_the_observed_band()
    {
        _atASpike = _strategy.Decide(State(120, CapacityKwh / 2), Quarter);
        _inATrough = _strategy.Decide(State(5, CapacityKwh / 2), Quarter);
        var middle = (_strategy.DischargeThresholdKw + _strategy.RechargeThresholdKw) / 2;
        _inTheMiddle = _strategy.Decide(State(middle, CapacityKwh / 2), Quarter);
    }

    [Fact] public void Should_discharge_at_a_spike_above_the_history() => _atASpike.Power.Value.ShouldBeLessThan(0);
    [Fact] public void Should_hold_a_discharge_threshold_below_the_spike() => _strategy.DischargeThresholdKw.ShouldBeLessThan(120);
    [Fact] public void Should_recharge_in_a_trough_below_the_history() => _inATrough.Power.Value.ShouldBeGreaterThan(0);
    [Fact] public void Should_hold_a_recharge_threshold_above_the_trough() => _strategy.RechargeThresholdKw.ShouldBeGreaterThan(5);
    [Fact] public void Should_rest_between_the_thresholds() => _inTheMiddle.ShouldBe(StorageSetpoint.Idle);
}
