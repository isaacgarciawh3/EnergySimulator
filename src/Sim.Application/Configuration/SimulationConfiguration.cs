namespace Sim.Application.Configuration;

/// <summary>
/// Everything the neighbourhood and the clock are built from. Persisted in
/// SQLite and editable on the configuration page — the whole simulation is a
/// pure function of this record (RNF determinism).
/// </summary>
public sealed record SimulationConfiguration(
    long Seed,
    DateTimeOffset StartInstant,
    int TickMinutes,
    double TicksPerSecond,
    double PvShare,
    double HeatPumpShare,
    double HomeEvShare,
    double BatteryCapacityKwh,
    double BatteryMaxPowerKw,
    double BatteryRoundTripEfficiency,
    double PeakShavingThresholdKw,
    bool BatteryEnabled)
{
    /// <summary>
    /// Last-resort fallback, used ONLY when appsettings.Simulation.json is
    /// absent. The file is the source of truth for a fresh boot (ADR-0012);
    /// these literals exist so the application still starts without it.
    /// </summary>
    public static readonly SimulationConfiguration Default = new(
        Seed: 20260818,
        StartInstant: new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
        TickMinutes: 15,
        TicksPerSecond: 8,
        PvShare: 0.40,
        HeatPumpShare: 0.30,
        HomeEvShare: 0.20,
        BatteryCapacityKwh: 250,
        BatteryMaxPowerKw: 80,
        BatteryRoundTripEfficiency: 0.90,
        PeakShavingThresholdKw: 0,
        BatteryEnabled: true);

    public TimeSpan TickDuration => TimeSpan.FromMinutes(TickMinutes);

    public SimulationConfiguration Validated() => this with
    {
        TickMinutes = Math.Clamp(TickMinutes, 1, 60),
        TicksPerSecond = Math.Clamp(TicksPerSecond, 0.5, 240),
        PvShare = Math.Clamp(PvShare, 0, 1),
        HeatPumpShare = Math.Clamp(HeatPumpShare, 0, 1),
        HomeEvShare = Math.Clamp(HomeEvShare, 0, 1),
        BatteryCapacityKwh = Math.Clamp(BatteryCapacityKwh, 0, 100_000),
        BatteryMaxPowerKw = Math.Clamp(BatteryMaxPowerKw, 0, 10_000),
        BatteryRoundTripEfficiency = Math.Clamp(BatteryRoundTripEfficiency, 0.1, 1.0),
        PeakShavingThresholdKw = Math.Clamp(PeakShavingThresholdKw, 0, 100_000),
    };
}
