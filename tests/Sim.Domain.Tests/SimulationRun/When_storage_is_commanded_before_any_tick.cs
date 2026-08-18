using Shouldly;
using Sim.Control.Domain;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>Invariant: storage is commanded only for a tick that has actually been advanced.</summary>
public class When_storage_is_commanded_before_any_tick
{
    private readonly Exception? _refusal =
        Record.Exception(() => RunOf(World(ADefaultBattery)).ApplyStorageSetpoint(StorageSetpoint.Idle));

    [Fact] public void Should_refuse_the_command() => _refusal.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_explain_that_no_tick_was_advanced() => _refusal!.Message.ShouldContain("has been advanced");
}
