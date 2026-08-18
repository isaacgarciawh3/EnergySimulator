namespace Sim.Domain.Simulation;

public enum Season { Winter, Spring, Summer, Autumn }

public sealed record WeatherSample(double TemperatureC, double CloudCover, double IrradianceFactor, Season Season);

/// <summary>
/// Deterministic synthetic weather (A-009): annual + diurnal temperature
/// sinusoids plus interpolated hash noise; cloud cover as smooth seeded noise
/// with a seasonal bias; irradiance as a clear-sky day-length bell attenuated
/// by cloud. No external API, reproducible from the seed alone.
/// </summary>
public sealed class WeatherModel(ulong seed)
{
    private const ulong TemperatureStream = 101;
    private const ulong CloudStream = 202;

    public WeatherSample Sample(DateTimeOffset instant)
    {
        var day = instant.DayOfYear;
        var hour = instant.TimeOfDay.TotalHours;

        var seasonalMean = 10.0 - 8.0 * Math.Cos(2 * Math.PI * (day - 15) / 365.0);
        var diurnal = 4.0 * Math.Sin(2 * Math.PI * (hour - 9) / 24.0);
        var temperature = seasonalMean + diurnal + 3.0 * (BlockNoise(TemperatureStream, instant, blockHours: 3) - 0.5);

        var cloudBias = 0.15 * Math.Cos(2 * Math.PI * (day - 15) / 365.0); // cloudier winters
        var cloud = Math.Clamp(0.9 * BlockNoise(CloudStream, instant, blockHours: 3) + cloudBias, 0.0, 1.0);

        var dayLength = 12.0 + 4.5 * Math.Cos(2 * Math.PI * (day - 172) / 365.0); // longest ~21 Jun
        var sunrise = 12.0 - dayLength / 2.0;
        var clearSky = Math.Max(0.0, Math.Sin(Math.PI * (hour - sunrise) / dayLength));
        var irradiance = Math.Pow(clearSky, 1.2) * (1.0 - 0.75 * cloud);

        return new WeatherSample(temperature, cloud, irradiance, SeasonOf(instant.Month));
    }

    public static Season SeasonOf(int month) => month switch
    {
        12 or 1 or 2 => Season.Winter,
        >= 3 and <= 5 => Season.Spring,
        >= 6 and <= 8 => Season.Summer,
        _ => Season.Autumn,
    };

    /// <summary>Piecewise-linear interpolation between per-block hash values keeps noise smooth.</summary>
    private double BlockNoise(ulong stream, DateTimeOffset instant, int blockHours)
    {
        var blockSeconds = blockHours * 3600L;
        var unix = instant.ToUnixTimeSeconds();
        var block = Math.DivRem(unix, blockSeconds, out var rest);
        var t = (double)rest / blockSeconds;
        var a = DeterministicNoise.Sample(seed, stream, block);
        var b = DeterministicNoise.Sample(seed, stream, block + 1);
        return a + (b - a) * t;
    }
}
