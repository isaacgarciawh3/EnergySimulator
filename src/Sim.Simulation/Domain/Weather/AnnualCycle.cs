namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// The one cosine both temperature and day length are built from: +1 on its
/// peak day, -1 half a year away, defined once and proven once. Pure function -
/// At() keeps its preposition under the fluent clause because the call site
/// reads as prose: AnnualCycle.At(day, peakDay).
/// </summary>
public static class AnnualCycle
{
    public static double At(int dayOfYear, int peakDayOfYear)
    {
        var phase = 2 * Math.PI * (dayOfYear - peakDayOfYear) / WeatherParameters.DaysPerYear;
        return Math.Cos(phase);
    }
}
