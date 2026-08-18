using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.CloudModelScenario;

/// <summary>Cloud cover is a fraction, whatever the day and noise - and winter is biased cloudier than summer.</summary>
public class When_cover_is_sampled_across_the_year
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _lowest = double.MaxValue;
    private readonly double _highest = double.MinValue;
    private readonly double _winterAtMidNoise;
    private readonly double _summerAtMidNoise;

    public When_cover_is_sampled_across_the_year()
    {
        foreach (var noise in new[] { 0.0, 0.5, 1.0 })
            for (var day = 1; day <= 365; day += 7)
            {
                var cover = Sim.Simulation.Domain.Weather.CloudModel.CoverTheSky(day, noise, P);
                _lowest = Math.Min(_lowest, cover);
                _highest = Math.Max(_highest, cover);
            }

        _winterAtMidNoise = Sim.Simulation.Domain.Weather.CloudModel.CoverTheSky(15, 0.5, P);
        _summerAtMidNoise = Sim.Simulation.Domain.Weather.CloudModel.CoverTheSky(196, 0.5, P);
    }

    [Fact] public void Should_never_drop_below_zero() => _lowest.ShouldBeGreaterThanOrEqualTo(0.0);
    [Fact] public void Should_never_exceed_one() => _highest.ShouldBeLessThanOrEqualTo(1.0);
    [Fact] public void Should_bias_winter_cloudier_than_summer_for_the_same_noise() => _winterAtMidNoise.ShouldBeGreaterThan(_summerAtMidNoise);
}
