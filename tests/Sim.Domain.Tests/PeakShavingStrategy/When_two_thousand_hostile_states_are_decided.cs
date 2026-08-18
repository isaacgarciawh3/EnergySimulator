using Shouldly;
using Sim.Control.Domain;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.PeakShavingStrategyScenario.StrategyScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>
/// Three observed days, then two thousand hostile states from deep export to
/// five times the peak at every state of charge - seeded by the same
/// deterministic noise the simulation uses, so a failure reproduces. Expensive,
/// so the fixture decides once and keeps the worst transgressions.
/// </summary>
public sealed class Two_thousand_hostile_decisions
{
    public double LargestCommandKw { get; private set; }
    public double WorstCellsOverdrawKwh { get; private set; } = double.MinValue;
    public double WorstOverfillKwh { get; private set; } = double.MinValue;

    public Two_thousand_hostile_decisions()
    {
        const ulong seed = 20260818;
        var strategy = new PeakShavingStrategy(fixedThresholdKw: 45, roundTripEfficiency: RoundTrip);

        foreach (var load in DailyLoadKw(days: 3))
            Record(strategy.Decide(State(load, CapacityKwh / 2), StrategyScenario.Quarter), State(load, CapacityKwh / 2));

        for (var i = 0; i < 2000; i++)
        {
            var net = -150.0 + 500.0 * DeterministicNoise.Sample(seed, 1, i);
            var soc = CapacityKwh * DeterministicNoise.Sample(seed, 2, i);
            var state = State(net, soc);
            Record(strategy.Decide(state, StrategyScenario.Quarter), state);
        }
    }

    private void Record(StorageSetpoint setpoint, GridState state)
    {
        LargestCommandKw = Math.Max(LargestCommandKw, Math.Abs(setpoint.Power.Value));
        if (setpoint.Power.Value < 0)
            WorstCellsOverdrawKwh = Math.Max(WorstCellsOverdrawKwh,
                -setpoint.Power.Value * QuarterHours / LegEfficiency - state.StateOfChargeKwh);
        if (setpoint.Power.Value > 0)
            WorstOverfillKwh = Math.Max(WorstOverfillKwh,
                state.StateOfChargeKwh + setpoint.Power.Value * QuarterHours * LegEfficiency - state.CapacityKwh);
    }
}

public class When_two_thousand_hostile_states_are_decided(Two_thousand_hostile_decisions sweep)
    : IClassFixture<Two_thousand_hostile_decisions>
{
    [Fact]
    public void Should_never_command_beyond_the_power_rating() =>
        sweep.LargestCommandKw.ShouldBeLessThanOrEqualTo(MaxPowerKw + AbsoluteTolerance);

    [Fact]
    public void Should_never_command_a_discharge_the_cells_cannot_deliver() =>
        sweep.WorstCellsOverdrawKwh.ShouldBeLessThanOrEqualTo(AbsoluteTolerance);

    [Fact]
    public void Should_never_command_a_charge_that_would_overfill() =>
        sweep.WorstOverfillKwh.ShouldBeLessThanOrEqualTo(AbsoluteTolerance);
}
