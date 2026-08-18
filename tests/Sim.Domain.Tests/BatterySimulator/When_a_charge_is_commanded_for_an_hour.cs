using Shouldly;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>ADR-0002 at the battery: charging is consumption, so the meter reads POSITIVE. Scenario: +30 kW for one hour.</summary>
public class When_a_charge_is_commanded_for_an_hour
{
    private readonly BatterySimulator _battery = Fresh();
    private readonly PowerReading _reading;

    public When_a_charge_is_commanded_for_an_hour() =>
        _reading = _battery.Apply(Command(30), Instant, Hour);

    [Fact] public void Should_meter_on_the_battery() => _reading.MeterId.ShouldBe("neighbourhood/battery");
    [Fact] public void Should_stamp_the_commanded_instant() => _reading.Instant.ShouldBe(Instant);
    [Fact] public void Should_meter_positive_power() => _reading.Power.Value.ShouldBeGreaterThan(0);
    [Fact] public void Should_hold_more_than_it_started_with() => _battery.StateOfChargeKwh.ShouldBeGreaterThan(CapacityKwh / 2);
}
