using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SolarGeometryScenario;

/// <summary>R-15: no clear sky outside daylight, a peak of exactly one at solar noon, and always a fraction.</summary>
public class When_the_sun_crosses_the_sky
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _beforeDawnInSummer = Sim.Simulation.Domain.Weather.SolarGeometry.RateTheClearSky(2.0, 172, P);
    private readonly double _lateEveningInSummer = Sim.Simulation.Domain.Weather.SolarGeometry.RateTheClearSky(23.5, 172, P);
    private readonly double _atSolarNoon = Sim.Simulation.Domain.Weather.SolarGeometry.RateTheClearSky(12.0, 172, P);
    private readonly double _summerNoon = Sim.Simulation.Domain.Weather.SolarGeometry.RateTheClearSky(12.0, 172, P);
    private readonly double _winterNoon = Sim.Simulation.Domain.Weather.SolarGeometry.RateTheClearSky(12.0, 15, P);
    private readonly double _lowestOfTheSweep = double.MaxValue;
    private readonly double _highestOfTheSweep = double.MinValue;

    public When_the_sun_crosses_the_sky()
    {
        for (var day = 1; day <= 365; day += 11)
            for (var hour = 0.0; hour < 24.0; hour += 0.25)
            {
                var factor = Sim.Simulation.Domain.Weather.SolarGeometry.RateTheClearSky(hour, day, P);
                _lowestOfTheSweep = Math.Min(_lowestOfTheSweep, factor);
                _highestOfTheSweep = Math.Max(_highestOfTheSweep, factor);
            }
    }

    [Fact] public void Should_produce_nothing_before_sunrise() => _beforeDawnInSummer.ShouldBe(0.0);
    [Fact] public void Should_produce_nothing_after_sunset() => _lateEveningInSummer.ShouldBe(0.0);
    [Fact] public void Should_peak_at_exactly_one_at_solar_noon() => _atSolarNoon.ShouldBe(1.0, 1e-9);
    [Fact] public void Should_never_go_negative_anywhere_in_the_year() => _lowestOfTheSweep.ShouldBeGreaterThanOrEqualTo(0.0);
    [Fact] public void Should_never_exceed_one_anywhere_in_the_year() => _highestOfTheSweep.ShouldBeLessThanOrEqualTo(1.0);
    [Fact] public void Should_give_winter_noon_no_more_than_summer_noon() => _winterNoon.ShouldBeLessThanOrEqualTo(_summerNoon);
}
