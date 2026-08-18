using Sim.Simulation.Domain.Weather;

namespace Sim.Simulation.Domain;

public sealed record WeatherConditions(double TemperatureC, double CloudCover, double IrradianceFactor, Season Season);

/// <summary>
/// Deterministic synthetic weather. This class now only COMPOSES the individual
/// rules - each of which lives in <see cref="Weather"/>, is named, is a pure
/// function, and is tested on its own.
///
/// The important property is unchanged: weather is a pure function of the
/// instant and the seed, never an accumulating random walk. The clock can
/// therefore jump forward and produce identical weather, which is what makes
/// the 24-hour warm-up at startup cheap and reproducible.
/// </summary>
public sealed class WeatherModel
{
    private const ulong TemperatureStream = 101;
    private const ulong CloudStream = 202;

    private readonly ulong _seed;
    private readonly WeatherParameters _p;

    public WeatherModel(ulong seed, WeatherParameters? parameters = null)
    {
        _seed = seed;
        _p = parameters ?? WeatherParameters.Default;
        _p.Validate();
    }

    public WeatherConditions At(DateTimeOffset instant)
    {
        var dayOfYear = instant.DayOfYear;
        var hourOfDay = instant.TimeOfDay.TotalHours;

        var temperature = TemperatureModel.Combine(dayOfYear, hourOfDay, Noise(TemperatureStream, instant), _p);
        var cloud = CloudModel.CoverFraction(dayOfYear, Noise(CloudStream, instant), _p);
        var clearSky = SolarGeometry.ClearSkyFactor(hourOfDay, dayOfYear, _p);
        var irradiance = SolarGeometry.IrradianceFactor(clearSky, cloud, _p);

        return new WeatherConditions(temperature, cloud, irradiance, Seasons.Of(instant.Month));
    }

    private double Noise(ulong stream, DateTimeOffset instant) =>
        SmoothNoise.At(_seed, stream, instant, _p.NoiseCorrelationPeriod);
}
