using Shouldly;
using Sim.Control.Domain;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>R-43 (the battery is optional): a run without one says so instead of pretending.</summary>
public class When_the_run_has_no_battery
{
    private readonly StorageState? _storage;
    private readonly Exception? _refusal;

    public When_the_run_has_no_battery()
    {
        var run = RunOf(World(battery: null));
        run.Advance();
        _storage = run.Storage;
        _refusal = Record.Exception(() => run.ApplyStorageSetpoint(StorageSetpoint.Idle));
    }

    [Fact] public void Should_have_no_storage_state_to_read() => _storage.ShouldBeNull();
    [Fact] public void Should_refuse_storage_commands() => _refusal.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_explain_that_there_is_no_battery() => _refusal!.Message.ShouldContain("no battery");
}
