using Sim.Application.Configuration;
using Sim.Application.ReadModels;

namespace Sim.Application.Ports;

/// <summary>
/// Repository over the persisted simulation configuration.
///
/// It answers questions about storage and nothing else. It used to be called a
/// store with a `LoadOrSeedDefault()` method, which quietly put a POLICY
/// decision - what should exist when nothing is stored - inside a SQLite
/// adapter that has no business knowing what a sensible default seed is. That
/// policy now lives in the application layer, where the defaults come from the
/// configuration file (ADR-0012).
/// </summary>
public interface ISimulationConfigurationRepository
{
    /// <summary>The stored configuration, or null when nothing has been stored yet.</summary>
    SimulationConfiguration? Find();

    void Save(SimulationConfiguration configuration);

    bool Exists();

    /// <summary>Forgets the stored configuration, so the next boot falls back to the file.</summary>
    void Clear();
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
