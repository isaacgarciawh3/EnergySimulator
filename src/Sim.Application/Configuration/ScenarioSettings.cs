namespace Sim.Application.Configuration;

/// <summary>
/// The scenario, as it appears in appsettings.Simulation.json: which world to
/// build and how fast to run it.
///
/// This is the half of the configuration a reader most wants to see, and until
/// now it was twelve C# literals. It is authoritative on a first boot; after
/// that a row persisted through the UI wins (ADR-0012).
///
/// What is deliberately NOT here: the house count and the public charger count.
/// The assignment states them as absolutes, so they are invariants enforced in
/// the domain, and a file that could change them would be a file that could
/// violate a requirement.
/// </summary>
public sealed class ScenarioSettings
{
    public const string SectionName = "Simulation:Scenario";

    public long Seed { get; init; } = 20260818;
    public string StartInstant { get; init; } = "2026-01-15T00:00:00+00:00";
    public int TickMinutes { get; init; } = 15;
    public double TicksPerSecond { get; init; } = 8;

    public double PvShare { get; init; } = 0.40;
    public double HeatPumpShare { get; init; } = 0.30;
    public double HomeEvShare { get; init; } = 0.20;

    public bool BatteryEnabled { get; init; } = true;
    public double BatteryCapacityKwh { get; init; } = 250;
    public double BatteryMaxPowerKw { get; init; } = 80;
    public double BatteryRoundTripEfficiency { get; init; } = 0.90;
    public double PeakShavingThresholdKw { get; init; }

    /// <summary>
    /// Fails the boot on a scenario that would produce a nonsense simulation,
    /// naming the offending field. A bad file should stop the application, not
    /// quietly run something plausible-looking.
    /// </summary>
    public SimulationConfiguration ToConfiguration()
    {
        if (!DateTimeOffset.TryParse(StartInstant, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            throw new InvalidOperationException(
                $"Scenario.StartInstant '{StartInstant}' is not a valid ISO-8601 instant.");

        Require(TickMinutes is >= 1 and <= 60, nameof(TickMinutes), "must be between 1 and 60 minutes");
        Require(TicksPerSecond is >= 0.5 and <= 240, nameof(TicksPerSecond), "must be between 0.5 and 240");
        Require(PvShare is >= 0 and <= 1, nameof(PvShare), "must be a fraction between 0 and 1");
        Require(HeatPumpShare is >= 0 and <= 1, nameof(HeatPumpShare), "must be a fraction between 0 and 1");
        Require(HomeEvShare is >= 0 and <= 1, nameof(HomeEvShare), "must be a fraction between 0 and 1");
        Require(BatteryCapacityKwh >= 0, nameof(BatteryCapacityKwh), "must not be negative");
        Require(BatteryMaxPowerKw >= 0, nameof(BatteryMaxPowerKw), "must not be negative");
        Require(BatteryRoundTripEfficiency is > 0 and <= 1, nameof(BatteryRoundTripEfficiency),
            "must be a fraction greater than 0 and at most 1");
        Require(PeakShavingThresholdKw >= 0, nameof(PeakShavingThresholdKw),
            "must not be negative (0 disables the hard ceiling)");

        return new SimulationConfiguration(
            Seed, start, TickMinutes, TicksPerSecond,
            PvShare, HeatPumpShare, HomeEvShare,
            BatteryCapacityKwh, BatteryMaxPowerKw, BatteryRoundTripEfficiency,
            PeakShavingThresholdKw, BatteryEnabled);
    }

    private static void Require(bool condition, string field, string requirement)
    {
        if (!condition) throw new InvalidOperationException($"Scenario.{field} {requirement}.");
    }
}
