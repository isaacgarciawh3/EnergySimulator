namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// Outdoor temperature as three independent, separately testable terms:
/// a seasonal mean, a daily swing around it, and noise.
/// </summary>
public static class TemperatureModel
{
    /// <summary>Mean temperature for the day of year, ignoring time of day.</summary>
    public static double SeasonalMeanC(int dayOfYear, WeatherParameters p) =>
        p.AnnualMeanC - p.AnnualAmplitudeC * AnnualCycle.At(dayOfYear, p.ColdestDayOfYear);

    /// <summary>Departure from the daily mean at this hour. Coldest before dawn, warmest in the afternoon.</summary>
    public static double DiurnalOffsetC(double hourOfDay, WeatherParameters p)
    {
        var phase = 2 * Math.PI * (hourOfDay - p.ColdestHourOfDay) / WeatherParameters.HoursPerDay;
        return -p.DiurnalAmplitudeC * Math.Cos(phase);
    }

    /// <summary>Weather variability, from a noise sample in [0,1) recentred to [-0.5, 0.5].</summary>
    public static double NoiseOffsetC(double unitNoise, WeatherParameters p) =>
        p.NoiseAmplitudeC * (unitNoise - 0.5);

    public static double Combine(int dayOfYear, double hourOfDay, double unitNoise, WeatherParameters p) =>
        SeasonalMeanC(dayOfYear, p) + DiurnalOffsetC(hourOfDay, p) + NoiseOffsetC(unitNoise, p);
}
