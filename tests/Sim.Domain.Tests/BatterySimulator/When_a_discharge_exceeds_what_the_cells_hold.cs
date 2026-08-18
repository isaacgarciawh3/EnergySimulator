using Shouldly;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>
/// TASK-016's rule "on discharge, the CELLS pay for the losses" where it bites:
/// asked for more than they hold, the cells deliver only their loss-adjusted
/// content and stop exactly at empty. Scenario: a half-full battery is
/// commanded a two-hour full-power discharge it cannot honour.
/// </summary>
public class When_a_discharge_exceeds_what_the_cells_hold
{
    private static readonly double LegEfficiency = Math.Sqrt(RoundTrip);
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);

    private readonly BatterySimulator _battery = Fresh();
    private readonly PowerReading _reading;
    private readonly double _expectedDeliveredKwh;

    public When_a_discharge_exceeds_what_the_cells_hold()
    {
        _expectedDeliveredKwh = _battery.StateOfChargeKwh * LegEfficiency;   // everything they can give
        _reading = _battery.Apply(Command(-MaxPowerKw), Instant, TwoHours);
    }

    [Fact]
    public void Should_deliver_only_what_the_cells_can_give() =>
        (-_reading.Power.Value * TwoHours.TotalHours).ShouldBe(_expectedDeliveredKwh,
            Close(-_reading.Power.Value * TwoHours.TotalHours, _expectedDeliveredKwh));

    [Fact]
    public void Should_empty_the_cells_exactly() =>
        _battery.StateOfChargeKwh.ShouldBe(0, AbsoluteTolerance);
}
