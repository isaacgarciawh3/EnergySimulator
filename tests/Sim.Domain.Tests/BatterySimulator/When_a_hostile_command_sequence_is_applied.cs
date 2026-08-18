using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>
/// A-010: the battery cannot store what it has no room for nor return what it
/// does not hold - whatever it is told. Scenario: five thousand commands of up
/// to four times the rating, both directions, random durations. Expensive, so
/// the fixture runs it once.
/// </summary>
public sealed class Five_thousand_hostile_commands
{
    public double LowestChargeKwh { get; private set; } = double.MaxValue;
    public double HighestChargeKwh { get; private set; } = double.MinValue;
    public double LowestPercent { get; private set; } = double.MaxValue;
    public double HighestPercent { get; private set; } = double.MinValue;

    public Five_thousand_hostile_commands()
    {
        const ulong seed = 20260818;
        var battery = Fresh();
        var instant = Instant;

        for (var i = 0; i < 5_000; i++)
        {
            var commanded = -200.0 + 400.0 * DeterministicNoise.Sample(seed, 3, i);
            var duration = TimeSpan.FromMinutes(1 + 59 * DeterministicNoise.Sample(seed, 4, i));
            battery.Apply(Command(commanded), instant, duration);
            instant += duration;

            LowestChargeKwh = Math.Min(LowestChargeKwh, battery.StateOfChargeKwh);
            HighestChargeKwh = Math.Max(HighestChargeKwh, battery.StateOfChargeKwh);
            LowestPercent = Math.Min(LowestPercent, battery.StateOfChargePercent);
            HighestPercent = Math.Max(HighestPercent, battery.StateOfChargePercent);
        }
    }
}

public class When_a_hostile_command_sequence_is_applied(Five_thousand_hostile_commands history)
    : IClassFixture<Five_thousand_hostile_commands>
{
    [Fact] public void Should_never_drop_below_empty() => history.LowestChargeKwh.ShouldBeGreaterThanOrEqualTo(-AbsoluteTolerance);
    [Fact] public void Should_never_exceed_capacity() => history.HighestChargeKwh.ShouldBeLessThanOrEqualTo(CapacityKwh + AbsoluteTolerance);
    [Fact] public void Should_never_report_a_negative_percentage() => history.LowestPercent.ShouldBeGreaterThanOrEqualTo(-AbsoluteTolerance);
    [Fact] public void Should_never_report_more_than_one_hundred_percent() => history.HighestPercent.ShouldBeLessThanOrEqualTo(100 + AbsoluteTolerance);
}
