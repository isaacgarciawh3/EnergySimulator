namespace Sim.Application.ReadModels;

/// <summary>Read-side model (CQRS): shaped for the dashboard, never for the domain.</summary>
public sealed record DashboardSnapshot(
    long TickIndex,
    DateTimeOffset Instant,
    string Season,
    double TemperatureC,
    double CloudCover,
    double IrradianceFactor,
    double NetPowerKw,
    double ConsumptionKw,
    double GenerationKw,
    double ImportKw,
    double ExportKw,
    double TotalConsumedKwh,
    double TotalGeneratedKwh,
    double TotalImportedKwh,
    double TotalExportedKwh,
    IReadOnlyList<MeterTotalView> Meters,
    IReadOnlyList<HouseView> Houses,
    IReadOnlyList<ChargerView> PublicChargers,
    IReadOnlyList<SeriesPoint> Last24Hours,
    bool Running,
    double TicksPerSecond,
    int TickMinutes,
    long Seed);

public sealed record MeterTotalView(string MeterId, string OwnerId, string Category, double ConsumedKwh, double GeneratedKwh, double NetKwh, double LastPowerKw);
public sealed record HouseView(string Id, IReadOnlyList<string> Assets, double NetPowerKw, double NetKwh);
public sealed record ChargerView(string Id, bool Busy, double PowerKw, double ConsumedKwh);
public sealed record SeriesPoint(DateTimeOffset Instant, double NetKw, double ConsumptionKw, double GenerationKw);
