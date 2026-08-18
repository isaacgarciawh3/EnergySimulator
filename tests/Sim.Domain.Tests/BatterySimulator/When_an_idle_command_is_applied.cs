using Shouldly;
using Sim.Control.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>An idle setpoint moves nothing and meters nothing - the do-no-harm case.</summary>
public class When_an_idle_command_is_applied
{
    private readonly BatterySimulator _battery = Fresh();
    private readonly PowerReading _reading;

    public When_an_idle_command_is_applied() =>
        _reading = _battery.Apply(StorageSetpoint.Idle, Instant, Hour);

    [Fact] public void Should_meter_zero_power() => _reading.Power.Value.ShouldBe(0, AbsoluteTolerance);
    [Fact] public void Should_leave_the_charge_untouched() => _battery.StateOfChargeKwh.ShouldBe(CapacityKwh / 2, AbsoluteTolerance);
}
