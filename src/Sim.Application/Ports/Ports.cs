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
/// Driven port: the projection store (CQRS read side). SQLite adapter today;
/// a real deployment would point this at Redis or a time-series database
/// without the domain noticing.
/// </summary>
public interface IProjectionStore
{
    void AppendTick(SeriesPoint point);
    void SaveMeterTotals(IReadOnlyList<MeterTotalView> meters);
    IReadOnlyList<SeriesPoint> LoadWindow(DateTimeOffset from);
    void Reset();
}

/// <summary>
/// Driven port standing in for the event stream we did NOT build (ADR-004).
/// Today the adapter is an in-process synchronous dispatch; the signature is
/// already the one a broker publisher would have, so replacing it is an
/// infrastructure change, not a domain change.
/// </summary>
public interface ITickBus
{
    void Publish(TickCompleted tick);
    void Subscribe(Action<TickCompleted> handler);
}

/// <summary>The single integration event of the system.</summary>
public sealed record TickCompleted(DashboardSnapshot Snapshot, SeriesPoint Point);
