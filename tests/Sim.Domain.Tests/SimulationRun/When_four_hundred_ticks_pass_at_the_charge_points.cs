using Shouldly;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>
/// RF-04/A-004 (6 public chargers, documented usage model): seeded arrivals
/// really occur, and occupancy never names anything but a public charge point.
/// The 400-tick history is expensive, so the fixture runs it once.
/// </summary>
public sealed class Four_hundred_ticks_of_charge_point_history
{
    public HashSet<string> EverReported { get; } = [];

    public Four_hundred_ticks_of_charge_point_history()
    {
        var run = RunOf(World());
        for (var i = 0; i < 400; i++)
            EverReported.UnionWith(run.Advance().OccupiedChargePoints);
    }
}

public class When_four_hundred_ticks_pass_at_the_charge_points(Four_hundred_ticks_of_charge_point_history history)
    : IClassFixture<Four_hundred_ticks_of_charge_point_history>
{
    [Fact]
    public void Should_occupy_at_least_one_public_charge_point() =>
        history.EverReported.ShouldNotBeEmpty();

    [Fact]
    public void Should_never_report_anything_but_a_public_charge_point() =>
        history.EverReported.ShouldAllBe(id => ChargePoints(6).Select(c => c.MeterId).Contains(id));
}
