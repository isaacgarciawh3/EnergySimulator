using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;

namespace Sim.Simulation.Behaviours;

/// <summary>
/// How one asset behaves over one interval. Signed power: consumption positive,
/// generation negative.
///
/// One instance per asset, because some behaviours carry state between ticks
/// (a car part-way through charging). That state is simulation state, not
/// energy-model state, which is exactly why it lives on this side of the
/// boundary.
/// </summary>
public interface IAssetBehaviour
{
    Kilowatts PowerAt(Asset asset, SimulationTick tick);
}
