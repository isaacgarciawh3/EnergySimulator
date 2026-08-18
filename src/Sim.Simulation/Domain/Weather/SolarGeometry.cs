namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// Where the sun is, and how much of it gets through. Pure geometry plus one
/// attenuation term - no noise, no state, no time zone.
/// </summary>
public static class SolarGeometry
{
    /// <summary>Hours of daylight, longest at the summer solstice.</summary>
    public static double DayLengthHours(int dayOfYear, WeatherParameters p) =>
        p.MeanDayLengthHours + p.DayLengthAmplitudeHours * AnnualCycle.At(dayOfYear, p.LongestDayOfYear);

    /// <summary>Daylight is centred on solar noon, so sunrise is half a day length before it.</summary>
    public static double SunriseHour(double dayLengthHours) => 12.0 - dayLengthHours / 2.0;

    public static double SunsetHour(double dayLengthHours) => 12.0 + dayLengthHours / 2.0;

    /// <summary>
    /// Clear-sky intensity in [0, 1]: zero before sunrise and after sunset, one
    /// at solar noon, a half-sine in between. The exponent shapes the shoulders.
    /// </summary>
    public static double ClearSkyFactor(double hourOfDay, int dayOfYear, WeatherParameters p)
    {
        var dayLength = DayLengthHours(dayOfYear, p);
        var sunrise = SunriseHour(dayLength);
        if (hourOfDay <= sunrise || hourOfDay >= SunsetHour(dayLength)) return 0.0;

        var elevation = Math.Sin(Math.PI * (hourOfDay - sunrise) / dayLength);
        return Math.Pow(Math.Max(0.0, elevation), p.ClearSkyExponent);
    }

    /// <summary>What actually reaches the panels after cloud, in [0, 1].</summary>
    public static double IrradianceFactor(double clearSkyFactor, double cloudFraction, WeatherParameters p) =>
        Math.Clamp(clearSkyFactor * (1.0 - p.CloudAttenuation * cloudFraction), 0.0, 1.0);
}
