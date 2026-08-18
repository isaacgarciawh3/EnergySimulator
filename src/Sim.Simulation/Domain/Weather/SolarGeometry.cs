namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// Where the sun is and how much of it gets through - pure geometry plus one
/// attenuation term. Day length swings around the solstice; daylight is centred
/// on solar noon; the clear sky is a half-sine between sunrise and sunset whose
/// exponent shapes the shoulders; cloud attenuates what reaches the panels,
/// bounded so irradiance can never go negative.
/// </summary>
public static class SolarGeometry
{
    public static double MeasureTheDayLengthHours(int dayOfYear, WeatherParameters p) =>
        p.MeanDayLengthHours + p.DayLengthAmplitudeHours * AnnualCycle.At(dayOfYear, p.LongestDayOfYear);

    public static double FindTheSunriseHour(double dayLengthHours) => 12.0 - dayLengthHours / 2.0;

    public static double FindTheSunsetHour(double dayLengthHours) => 12.0 + dayLengthHours / 2.0;

    public static double RateTheClearSky(double hourOfDay, int dayOfYear, WeatherParameters p)
    {
        var dayLength = MeasureTheDayLengthHours(dayOfYear, p);
        var sunrise = FindTheSunriseHour(dayLength);
        if (hourOfDay <= sunrise || hourOfDay >= FindTheSunsetHour(dayLength)) return 0.0;

        var elevation = Math.Sin(Math.PI * (hourOfDay - sunrise) / dayLength);
        return Math.Pow(Math.Max(0.0, elevation), p.ClearSkyExponent);
    }

    public static double AttenuateByCloud(double clearSkyFactor, double cloudFraction, WeatherParameters p) =>
        Math.Clamp(clearSkyFactor * (1.0 - p.CloudAttenuation * cloudFraction), 0.0, 1.0);
}
