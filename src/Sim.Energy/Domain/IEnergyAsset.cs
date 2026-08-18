using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain;

/// <summary>
/// STRATEGY: the single call signature every asset answers, whatever its
/// physics. This is why heat pumps, PV and EV chargers can be optional per
/// house without the caller ever branching on type — adding a battery tomorrow
/// means adding one class, and touching nothing else.
/// </summary>
public interface IEnergyAsset
{
    string MeterId { get; }
    string OwnerId { get; }
    AssetType Type { get; }
    Kilowatts Measure(MeasurementContext ctx);
}
