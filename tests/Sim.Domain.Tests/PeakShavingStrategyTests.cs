using Shouldly;
using Sim.Control.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests;

/// <summary>
/// The controller is a pure function of a GridState, which makes it the cheapest
/// thing in the system to test exhaustively - and the most dangerous thing to get
/// wrong, because a bad setpoint is a command sent to real hardware.
///
/// The strategy is adaptive: it decides against percentiles of the load it has
/// actually observed, so every test here feeds it a realistic day before asserting.
/// A single call has no history and by construction can only be Idle.
/// </summary>
public sealed class PeakShavingStrategyTests
{
    private const double CapacityKwh = 250;
    private const double MaxPowerKw = 80;
    private const double RoundTrip = 0.9;

    private static readonly double LegEfficiency = Math.Sqrt(RoundTrip);
    private static readonly TimeSpan Quarter = TimeSpan.FromMinutes(15);
    private static readonly double QuarterHours = Quarter.TotalHours;

    /// <summary>A plausible winter day for thirty houses: night trough, morning and evening peaks.</summary>
    private static IEnumerable<double> DailyLoadKw(int days = 1)
    {
        for (var day = 0; day < days; day++)
            for (var slot = 0; slot < 96; slot++)
            {
                var hour = slot / 4.0;
                var shape = hour switch { < 6 => 0.55, < 9 => 1.50, < 17 => 0.90, < 22 => 1.80, _ => 0.80 };
                var wobble = 1.0 + 0.08 * Math.Sin(2 * Math.PI * (slot + 13 * day) / 17.0);
                yield return 35.0 * shape * wobble;
            }
    }

    private static GridState State(double netKw, double socKwh) =>
        new(new Kilowatts(netKw), socKwh, CapacityKwh, MaxPowerKw);

    /// <summary>Feeds one observed day so the percentile window is populated. Returns the warmed strategy.</summary>
    private static PeakShavingStrategy Warmed(double socKwh = CapacityKwh / 2, double? fixedThresholdKw = null)
    {
        var strategy = new PeakShavingStrategy(fixedThresholdKw, RoundTrip);
        foreach (var load in DailyLoadKw()) strategy.Decide(State(load, socKwh), Quarter);
        return strategy;
    }

    // 12
    [Fact]
    public void It_discharges_when_net_load_is_high_relative_to_observed_history()
    {
        var strategy = Warmed();

        var setpoint = strategy.Decide(State(netKw: 120, socKwh: CapacityKwh / 2), Quarter);

        setpoint.Power.Value.ShouldBeLessThan(0);
        strategy.DischargeThresholdKw.ShouldBeLessThan(120);
    }

    // 12
    [Fact]
    public void It_recharges_when_net_load_is_low_relative_to_observed_history()
    {
        var strategy = Warmed();

        var setpoint = strategy.Decide(State(netKw: 5, socKwh: CapacityKwh / 2), Quarter);

        setpoint.Power.Value.ShouldBeGreaterThan(0);
        strategy.RechargeThresholdKw.ShouldBeGreaterThan(5);
    }

    // 12
    [Fact]
    public void It_stays_idle_at_a_load_between_the_two_thresholds()
    {
        var strategy = Warmed();
        var middle = (strategy.DischargeThresholdKw + strategy.RechargeThresholdKw) / 2;

        var setpoint = strategy.Decide(State(middle, CapacityKwh / 2), Quarter);

        setpoint.ShouldBe(StorageSetpoint.Idle);
    }

    // 12
    [Fact]
    public void With_no_observed_history_nothing_can_be_a_peak_so_the_first_decision_is_idle()
    {
        var strategy = new PeakShavingStrategy(roundTripEfficiency: RoundTrip);

        strategy.Decide(State(netKw: 500, socKwh: CapacityKwh / 2), Quarter).ShouldBe(StorageSetpoint.Idle);
    }

    // 13, 14, 15 - one adversarial sweep, three independent claims.
    [Fact]
    public void It_never_commands_more_than_the_power_rating_in_either_direction()
    {
        foreach (var (setpoint, _) in AdversarialSweep())
            Math.Abs(setpoint.Power.Value).ShouldBeLessThanOrEqualTo(MaxPowerKw + AbsoluteTolerance);
    }

    // 14
    [Fact]
    public void It_never_commands_a_discharge_the_stored_energy_cannot_deliver()
    {
        foreach (var (setpoint, state) in AdversarialSweep())
        {
            if (setpoint.Power.Value >= 0) continue;

            // Metered energy out over the interval; the cells give up more than that, losses included.
            var deliveredKwh = -setpoint.Power.Value * QuarterHours;
            var drawnFromCellsKwh = deliveredKwh / LegEfficiency;

            drawnFromCellsKwh.ShouldBeLessThanOrEqualTo(state.StateOfChargeKwh + AbsoluteTolerance);
        }
    }

    // 14
    [Fact]
    public void An_empty_battery_is_idle_however_high_the_peak()
    {
        var strategy = Warmed(socKwh: 0);

        foreach (var peak in new[] { 100.0, 250.0, 5_000.0 })
            strategy.Decide(State(peak, socKwh: 0), Quarter).ShouldBe(StorageSetpoint.Idle);
    }

    // 15
    [Fact]
    public void It_never_commands_a_charge_that_would_overfill_the_battery()
    {
        foreach (var (setpoint, state) in AdversarialSweep())
        {
            if (setpoint.Power.Value <= 0) continue;

            // Metered energy in over the interval; the cells keep less than that, losses included.
            var meteredKwh = setpoint.Power.Value * QuarterHours;
            var storedKwh = meteredKwh * LegEfficiency;

            (state.StateOfChargeKwh + storedKwh).ShouldBeLessThanOrEqualTo(state.CapacityKwh + AbsoluteTolerance);
        }
    }

    // 15
    [Fact]
    public void A_full_battery_is_idle_however_low_the_load()
    {
        var strategy = Warmed(socKwh: CapacityKwh);

        foreach (var trough in new[] { 0.0, 5.0, -200.0 })
            strategy.Decide(State(trough, socKwh: CapacityKwh), Quarter).ShouldBe(StorageSetpoint.Idle);
    }

    /// <summary>
    /// Three observed days of realistic load, then 2000 hostile states: loads from
    /// deep export to five times the observed peak, at every state of charge from
    /// empty to full. Seeded from the same deterministic noise the simulation uses,
    /// so a failure here is reproducible rather than a once-a-month flake.
    /// </summary>
    private static IEnumerable<(StorageSetpoint Setpoint, GridState State)> AdversarialSweep()
    {
        const ulong seed = 20260818;
        var strategy = new PeakShavingStrategy(fixedThresholdKw: 45, roundTripEfficiency: RoundTrip);

        foreach (var load in DailyLoadKw(days: 3))
            yield return (strategy.Decide(State(load, CapacityKwh / 2), Quarter), State(load, CapacityKwh / 2));

        for (var i = 0; i < 2000; i++)
        {
            var net = -150.0 + 500.0 * DeterministicNoise.Sample(seed, 1, i);
            var soc = CapacityKwh * DeterministicNoise.Sample(seed, 2, i);
            var state = State(net, soc);
            yield return (strategy.Decide(state, Quarter), state);
        }
    }
}
