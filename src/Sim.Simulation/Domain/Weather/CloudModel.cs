namespace Sim.Simulation.Domain.Weather;

/// <summary>Cloud cover as a fraction in [0, 1]: smoothed noise, biased cloudier in winter.</summary>
public static class CloudModel
{
    public static double CoverFraction(int dayOfYear, double unitNoise, WeatherParameters p)
    {
        var winterBias = p.WinterCloudBias * AnnualCycle.At(dayOfYear, p.ColdestDayOfYear);
        return Math.Clamp(p.CloudNoiseScale * unitNoise + winterBias, 0.0, 1.0);
    }
}
