using Shouldly;
using Sim.Control.Domain;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>Invariant: storage is commanded at most once per tick - a double command is a bug in the caller.</summary>
public class When_storage_is_commanded_twice_for_the_same_tick
{
    private readonly Exception? _secondCommand;

    public When_storage_is_commanded_twice_for_the_same_tick()
    {
        var run = RunOf(World(ADefaultBattery));
        run.Advance();
        run.ApplyStorageSetpoint(StorageSetpoint.Idle);
        _secondCommand = Record.Exception(() => run.ApplyStorageSetpoint(StorageSetpoint.Idle));
    }

    [Fact] public void Should_refuse_the_second_command() => _secondCommand.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_explain_it_was_already_commanded() => _secondCommand!.Message.ShouldContain("already commanded");
}
