namespace Sim.Domain.Contracts;

public enum AssetType
{
    BaseLoad,
    HeatPump,
    Pv,
    HomeEvCharger,
    PublicEvCharger,
}

/// <summary>
/// The published contract between the Simulation and Accounting bounded
/// contexts (ADR-001, A-001): every asset is a meter-like source of power
/// measurements. Downstream consumers see readings, never asset internals.
/// </summary>
public sealed record MeterReading(
    string MeterId,
    string OwnerId,
    AssetType Type,
    DateTimeOffset Instant,
    Kilowatts Power,
    KilowattHours Energy);

/// <summary>Grid settlement for one tick. Import and export are mutually exclusive.</summary>
public sealed record GridFlow(
    Kilowatts Net,
    Kilowatts Import,
    Kilowatts Export,
    KilowattHours ImportedEnergy,
    KilowattHours ExportedEnergy);

/// <summary>Weather as published to consumers (no Simulation types leak).</summary>
public sealed record WeatherReport(double TemperatureC, double CloudCover, double IrradianceFactor, string Season);

/// <summary>Everything a tick produced. The unit of publication on the tick bus.</summary>
public sealed record TickReport(
    long TickIndex,
    DateTimeOffset Instant,
    TimeSpan Duration,
    IReadOnlyList<MeterReading> Readings,
    GridFlow Grid,
    WeatherReport Weather);
