namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// Every constant the weather model uses, named and validated.
///
/// These were previously fifteen unnamed literals inside one method. Naming them
/// is not decoration: it is what makes each rule reviewable, independently
/// testable, and configurable from appsettings.Simulation.json.
/// </summary>
public sealed record WeatherParameters(
    // Temperature: an annual cycle plus a daily cycle plus noise.
    double AnnualMeanC,
    double AnnualAmplitudeC,
    int ColdestDayOfYear,
    double DiurnalAmplitudeC,
    double ColdestHourOfDay,
    double NoiseAmplitudeC,

    // Cloud cover: smoothed noise, biased cloudier in winter.
    double CloudNoiseScale,
    double WinterCloudBias,

    // How long the weather stays correlated. Below this, conditions blend
    // smoothly rather than jumping from one interval to the next.
    double NoiseCorrelationHours,

    // Solar geometry: day length swings around the solstice.
    double MeanDayLengthHours,
    double DayLengthAmplitudeHours,
    int LongestDayOfYear,

    // Irradiance: a clear-sky curve, attenuated by cloud.
    double ClearSkyExponent,
    double CloudAttenuation)
{
    public const int DaysPerYear = 365;
    public const double HoursPerDay = 24.0;

    /// <summary>Northern-hemisphere maritime climate, roughly the Netherlands.</summary>
    public static readonly WeatherParameters Default = new(
        AnnualMeanC: 10.0,
        AnnualAmplitudeC: 8.0,
        ColdestDayOfYear: 15,          // mid-January
        DiurnalAmplitudeC: 4.0,
        ColdestHourOfDay: 3.0,         // coldest just before dawn
        NoiseAmplitudeC: 3.0,
        CloudNoiseScale: 0.9,
        WinterCloudBias: 0.15,
        NoiseCorrelationHours: 3.0,
        MeanDayLengthHours: 12.0,
        DayLengthAmplitudeHours: 4.5,
        LongestDayOfYear: 172,         // ~21 June
        ClearSkyExponent: 1.2,
        CloudAttenuation: 0.75);

    /// <summary>
    /// Fails loudly on values that would silently produce a nonsense climate -
    /// a negative day length, or cloud that could push irradiance below zero.
    /// </summary>
    public void Validate()
    {
        Require(AnnualAmplitudeC >= 0, nameof(AnnualAmplitudeC), "must not be negative");
        Require(DiurnalAmplitudeC >= 0, nameof(DiurnalAmplitudeC), "must not be negative");
        Require(NoiseAmplitudeC >= 0, nameof(NoiseAmplitudeC), "must not be negative");
        Require(ColdestDayOfYear is >= 1 and <= DaysPerYear, nameof(ColdestDayOfYear), "must be a day of the year");
        Require(LongestDayOfYear is >= 1 and <= DaysPerYear, nameof(LongestDayOfYear), "must be a day of the year");
        Require(ColdestHourOfDay is >= 0 and < HoursPerDay, nameof(ColdestHourOfDay), "must be an hour of the day");
        Require(CloudNoiseScale is >= 0 and <= 1, nameof(CloudNoiseScale), "must be within [0, 1]");
        Require(Math.Abs(WinterCloudBias) <= 1, nameof(WinterCloudBias), "must be within [-1, 1]");
        Require(ClearSkyExponent > 0, nameof(ClearSkyExponent), "must be positive");
        Require(NoiseCorrelationHours > 0, nameof(NoiseCorrelationHours), "must be positive");
        Require(CloudAttenuation is >= 0 and <= 1, nameof(CloudAttenuation), "must be within [0, 1]");
        Require(DayLengthAmplitudeHours >= 0, nameof(DayLengthAmplitudeHours), "must not be negative");
        Require(MeanDayLengthHours - DayLengthAmplitudeHours > 0, nameof(DayLengthAmplitudeHours),
            "must be smaller than the mean day length, otherwise the shortest day has no daylight");
        Require(MeanDayLengthHours + DayLengthAmplitudeHours < HoursPerDay, nameof(DayLengthAmplitudeHours),
            "must keep the longest day under 24 hours");
    }

    public TimeSpan NoiseCorrelationPeriod => TimeSpan.FromHours(NoiseCorrelationHours);

    private static void Require(bool condition, string name, string requirement)
    {
        if (!condition) throw new Sim.Simulation.Domain.SimulationInvariantViolation($"WeatherParameters.{name} {requirement}.");
    }
}
