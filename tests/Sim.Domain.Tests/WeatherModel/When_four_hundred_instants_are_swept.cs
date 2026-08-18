using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>Whatever the instant, the reported values stay inside their physical ranges.</summary>
public class When_four_hundred_instants_are_swept
{
    private readonly double _lowestCloud = double.MaxValue;
    private readonly double _highestCloud = double.MinValue;
    private readonly double _lowestIrradiance = double.MaxValue;
    private readonly double _highestIrradiance = double.MinValue;
    private readonly double _coldest = double.MaxValue;
    private readonly double _warmest = double.MinValue;

    public When_four_hundred_instants_are_swept()
    {
        var model = new WeatherModel(ConfiguredSeed);
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 400; i++)
        {
            var weather = model.At(start.AddHours(i * 2.3));
            _lowestCloud = Math.Min(_lowestCloud, weather.CloudCover);
            _highestCloud = Math.Max(_highestCloud, weather.CloudCover);
            _lowestIrradiance = Math.Min(_lowestIrradiance, weather.IrradianceFactor);
            _highestIrradiance = Math.Max(_highestIrradiance, weather.IrradianceFactor);
            _coldest = Math.Min(_coldest, weather.TemperatureC);
            _warmest = Math.Max(_warmest, weather.TemperatureC);
        }
    }

    [Fact] public void Should_keep_cloud_cover_at_or_above_zero() => _lowestCloud.ShouldBeGreaterThanOrEqualTo(0.0);
    [Fact] public void Should_keep_cloud_cover_at_or_below_one() => _highestCloud.ShouldBeLessThanOrEqualTo(1.0);
    [Fact] public void Should_keep_irradiance_at_or_above_zero() => _lowestIrradiance.ShouldBeGreaterThanOrEqualTo(0.0);
    [Fact] public void Should_keep_irradiance_at_or_below_one() => _highestIrradiance.ShouldBeLessThanOrEqualTo(1.0);
    [Fact] public void Should_never_freeze_past_a_plausible_climate() => _coldest.ShouldBeGreaterThan(-40.0);
    [Fact] public void Should_never_boil_past_a_plausible_climate() => _warmest.ShouldBeLessThan(50.0);
}
