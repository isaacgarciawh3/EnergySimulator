using Sim.Application.Configuration;
using Sim.Application.ReadModels;

namespace Sim.Application.Ports;

/// <summary>Driven port: where the configuration lives. SQLite adapter today.</summary>
public interface ISimulationConfigurationStore
{
    SimulationConfiguration LoadOrSeedDefault();
    void Save(SimulationConfiguration configuration);
}

/// <summary>
/// Driven port: the read-side projection. SQLite adapter today; a real
/// deployment would point this at a time-series store without the domain
/// noticing.
/// </summary>
public interface IProjectionStore
{
    void AppendTick(SeriesPoint point);
    void SaveMeterTotals(IReadOnlyList<MeterTotalView> meters);
    IReadOnlyList<SeriesPoint> LoadWindow(DateTimeOffset from);
    void Reset();
}
