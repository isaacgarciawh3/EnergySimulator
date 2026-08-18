using Shouldly;
using Sim.Control.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>
/// R-44/R-46 (the battery responds to control): the setpoint is applied over the
/// tick just advanced and the battery's meter reports what actually happened.
/// </summary>
public class When_storage_is_commanded_for_the_advanced_tick
{
    private readonly TickTelemetry _telemetry;
    private readonly PowerReading _reading;

    public When_storage_is_commanded_for_the_advanced_tick()
    {
        var run = RunOf(World(ADefaultBattery));
        _telemetry = run.Advance();
        _reading = run.ApplyStorageSetpoint(new StorageSetpoint(new Kilowatts(10)));
    }

    [Fact] public void Should_meter_the_reading_on_the_battery() => _reading.MeterId.ShouldBe("neighbourhood/battery");
    [Fact] public void Should_stamp_the_reading_with_the_advanced_instant() => _reading.Instant.ShouldBe(_telemetry.Instant);

    [Fact]
    public void Should_absorb_the_commanded_power() =>
        _reading.Power.Value.ShouldBe(10, Close(10, _reading.Power.Value));
}
