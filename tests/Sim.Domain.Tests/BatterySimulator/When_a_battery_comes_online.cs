using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>A-010: the battery starts half full so the first peak has something to shave.</summary>
public class When_a_battery_comes_online
{
    private readonly BatterySimulator _battery = Fresh();

    [Fact] public void Should_hold_half_its_capacity() => _battery.StateOfChargeKwh.ShouldBe(CapacityKwh / 2, AbsoluteTolerance);
    [Fact] public void Should_report_the_nameplate_capacity() => _battery.CapacityKwh.ShouldBe(CapacityKwh, AbsoluteTolerance);
    [Fact] public void Should_stand_at_fifty_percent() => _battery.StateOfChargePercent.ShouldBe(50, AbsoluteTolerance);
}
