using Shouldly;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>
/// TASK-016's rule "on charge, the METER pays for the losses" where it bites:
/// near a full battery the meter may only push what the remaining room absorbs,
/// loss-adjusted. Scenario: a battery is charged close to full, then commanded
/// a full-power hour it has no room for. Expected values are computed from the
/// nameplate and the leg efficiency, never copied from the output.
/// </summary>
public class When_a_charge_is_commanded_near_a_full_battery
{
    private static readonly double LegEfficiency = Math.Sqrt(RoundTrip);

    private readonly BatterySimulator _battery = Fresh();
    private readonly double _chargeBeforeTheLastCommandKwh;
    private readonly PowerReading _lastReading;
    private readonly double _expectedMeteredKwh;

    public When_a_charge_is_commanded_near_a_full_battery()
    {
        _battery.Apply(Command(MaxPowerKw), Instant, Hour);        // 50 kWh metered: cells now near full
        _chargeBeforeTheLastCommandKwh = _battery.StateOfChargeKwh;

        _expectedMeteredKwh = (CapacityKwh - _chargeBeforeTheLastCommandKwh) / LegEfficiency;
        _lastReading = _battery.Apply(Command(MaxPowerKw), Instant + Hour, Hour);
    }

    [Fact]
    public void Should_deliver_only_what_the_remaining_room_absorbs() =>
        _lastReading.Power.Value.ShouldBe(_expectedMeteredKwh,
            Close(_lastReading.Power.Value, _expectedMeteredKwh));

    [Fact]
    public void Should_deliver_less_than_was_commanded() =>
        _lastReading.Power.Value.ShouldBeLessThan(MaxPowerKw);

    [Fact]
    public void Should_fill_the_cells_exactly_to_capacity() =>
        _battery.StateOfChargeKwh.ShouldBe(CapacityKwh, Close(_battery.StateOfChargeKwh, CapacityKwh));
}
