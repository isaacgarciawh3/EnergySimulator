using Sim.Energy.Domain;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>Shared vocabulary for the SimulationRun scenarios.</summary>
internal static class RunScenario
{
    public static Neighbourhood World(Battery? battery = null) => new(Houses(30), ChargePoints(6), battery);

    public static SimulationRun RunOf(Neighbourhood world) => new(world, seed: 42, Instant, Quarter);

    public static Battery ADefaultBattery => new("neighbourhood/battery", CapacityKwh: 100, MaxPowerKw: 50);
}
