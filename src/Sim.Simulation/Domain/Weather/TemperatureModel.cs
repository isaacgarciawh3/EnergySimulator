namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// Outdoor temperature as three independent pure terms, combined by addition:
/// the seasonal mean for the day of year, the diurnal swing around it (coldest
/// before dawn, warmest mid afternoon), and centred noise that shifts nothing
/// at a mid sample and swings half the amplitude at the extremes.
/// </summary>
public static class TemperatureModel
{
    public static double AverageTheSeasonalTemperatureC(int dayOfYear, WeatherParameters p) =>
        p.AnnualMeanC - p.AnnualAmplitudeC * AnnualCycle.At(dayOfYear, p.ColdestDayOfYear);

    public static double OffsetByTimeOfDayC(double hourOfDay, WeatherParameters p)
    {
        var phase = 2 * Math.PI * (hourOfDay - p.ColdestHourOfDay) / WeatherParameters.HoursPerDay;
        return -p.DiurnalAmplitudeC * Math.Cos(phase);
    }

    public static double OffsetByNoiseC(double unitNoise, WeatherParameters p) =>
        p.NoiseAmplitudeC * (unitNoise - 0.5);

    public static double Combine(int dayOfYear, double hourOfDay, double unitNoise, WeatherParameters p) =>
        AverageTheSeasonalTemperatureC(dayOfYear, p) + OffsetByTimeOfDayC(hourOfDay, p) + OffsetByNoiseC(unitNoise, p);
}
