using Sim.Control.Domain;
using Shouldly;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>Invariant: the once-per-tick rule resets with the clock - each new tick may command storage again.</summary>
public class When_the_next_tick_is_advanced_after_a_storage_command
{
    private readonly Exception? _commandOnTheNewTick;

    public When_the_next_tick_is_advanced_after_a_storage_command()
    {
        var run = RunOf(World(ADefaultBattery));
        run.Advance();
        run.ApplyStorageSetpoint(StorageSetpoint.Idle);
        run.Advance();
        _commandOnTheNewTick = Record.Exception(() => run.ApplyStorageSetpoint(StorageSetpoint.Idle));
    }

    [Fact] public void Should_accept_a_new_storage_command() => _commandOnTheNewTick.ShouldBeNull();
}
