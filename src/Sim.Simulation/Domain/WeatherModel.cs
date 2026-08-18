using Sim.SharedKernel;

namespace Sim.Simulation.Domain;

public sealed record WeatherConditions(double TemperatureC, double CloudCover, double IrradianceFactor, Season Season);

/// <summary>
/// Deterministic synthetic weather (A-009). Weather is a PURE FUNCTION of the
/// instant and the seed — never an accumulating random walk — so the clock can
/// jump forward and still produce identical weather. Annual + diurnal
/// temperature sinusoids, smooth seeded cloud cover, and a clear-sky
/// day-length bell attenuated by cloud.
/// </summary>
public sealed class WeatherModel(ulong seed)
{
    private const ulong TemperatureStream = 101;
    private const ulong CloudStream = 202;

    public WeatherConditions At(DateTimeOffset instant)
    {
        var day = instant.DayOfYear;
        var hour = instant.TimeOfDay.TotalHours;

        var seasonalMean = 10.0 - 8.0 * Math.Cos(2 * Math.PI * (day - 15) / 365.0);
        var diurnal = 4.0 * Math.Sin(2 * Math.PI * (hour - 9) / 24.0);
        var temperature = seasonalMean + diurnal + 3.0 * (Smooth(TemperatureStream, instant) - 0.5);

        var cloudBias = 0.15 * Math.Cos(2 * Math.PI * (day - 15) / 365.0); // cloudier in winter
        var cloud = Math.Clamp(0.9 * Smooth(CloudStream, instant) + cloudBias, 0.0, 1.0);

        var dayLength = 12.0 + 4.5 * Math.Cos(2 * Math.PI * (day - 172) / 365.0);
        var sunrise = 12.0 - dayLength / 2.0;
        var clearSky = Math.Max(0.0, Math.Sin(Math.PI * (hour - sunrise) / dayLength));
        var irradiance = Math.Pow(clearSky, 1.2) * (1.0 - 0.75 * cloud);

        return new WeatherConditions(temperature, cloud, irradiance, Seasons.Of(instant.Month));
    }

    /// <summary>Linear interpolation between 3-hour block hashes keeps the series continuous.</summary>
    private double Smooth(ulong stream, DateTimeOffset instant)
    {
        const long blockSeconds = 3 * 3600L;
        var block = Math.DivRem(instant.ToUnixTimeSeconds(), blockSeconds, out var rest);
        var t = (double)rest / blockSeconds;
        var a = DeterministicNoise.Sample(seed, stream, block);
        var b = DeterministicNoise.Sample(seed, stream, block + 1);
        return a + (b - a) * t;
    }
}
