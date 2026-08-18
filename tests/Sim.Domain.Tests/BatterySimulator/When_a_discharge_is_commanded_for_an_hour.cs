using Shouldly;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>ADR-0002 at the battery: discharging is generation, so the meter reads NEGATIVE. Scenario: -30 kW for one hour.</summary>
public class When_a_discharge_is_commanded_for_an_hour
{
    private readonly BatterySimulator _battery = Fresh();
    private readonly PowerReading _reading;

    public When_a_discharge_is_commanded_for_an_hour() =>
        _reading = _battery.Apply(Command(-30), Instant, Hour);

    [Fact] public void Should_meter_negative_power() => _reading.Power.Value.ShouldBeLessThan(0);
    [Fact] public void Should_hold_less_than_it_started_with() => _battery.StateOfChargeKwh.ShouldBeLessThan(CapacityKwh / 2);
}
