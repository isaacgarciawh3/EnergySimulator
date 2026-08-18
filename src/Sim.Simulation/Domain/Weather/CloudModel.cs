namespace Sim.Simulation.Domain.Weather;

/// <summary>Cloud cover as a fraction in [0, 1]: scaled noise plus a bias that makes winter cloudier than summer.</summary>
public static class CloudModel
{
    public static double CoverTheSky(int dayOfYear, double unitNoise, WeatherParameters p)
    {
        var winterBias = p.WinterCloudBias * AnnualCycle.At(dayOfYear, p.ColdestDayOfYear);
        return Math.Clamp(p.CloudNoiseScale * unitNoise + winterBias, 0.0, 1.0);
    }
}
