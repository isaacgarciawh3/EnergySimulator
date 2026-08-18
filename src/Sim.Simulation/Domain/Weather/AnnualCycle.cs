namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// A cosine that peaks on a chosen day of the year. Both the temperature curve
/// and the day-length curve are the same shape with different arguments, so it
/// exists once and is tested once.
/// </summary>
public static class AnnualCycle
{
    /// <summary>
    /// Returns a value in [-1, 1]: +1 on <paramref name="peakDayOfYear"/>,
    /// -1 half a year away.
    /// </summary>
    public static double At(int dayOfYear, int peakDayOfYear)
    {
        var phase = 2 * Math.PI * (dayOfYear - peakDayOfYear) / WeatherParameters.DaysPerYear;
        return Math.Cos(phase);
    }
}
