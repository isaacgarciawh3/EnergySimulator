using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SolarGeometryScenario;

/// <summary>Seasonality lives in day length: longest at the solstice, always a sane number of hours, symmetric about noon.</summary>
public class When_day_length_follows_the_calendar
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _atTheSolstice = Sim.Simulation.Domain.Weather.SolarGeometry.DayLengthHours(P.LongestDayOfYear, P);
    private readonly double _shortest = double.MaxValue;
    private readonly double _longest = double.MinValue;
    private readonly double _sunrisePlusSunset;

    public When_day_length_follows_the_calendar()
    {
        for (var day = 1; day <= 365; day++)
        {
            var length = Sim.Simulation.Domain.Weather.SolarGeometry.DayLengthHours(day, P);
            _shortest = Math.Min(_shortest, length);
            _longest = Math.Max(_longest, length);
        }
        var length100 = Sim.Simulation.Domain.Weather.SolarGeometry.DayLengthHours(100, P);
        _sunrisePlusSunset = Sim.Simulation.Domain.Weather.SolarGeometry.SunriseHour(length100)
                           + Sim.Simulation.Domain.Weather.SolarGeometry.SunsetHour(length100);
    }

    [Fact]
    public void Should_make_the_solstice_the_longest_day() =>
        _atTheSolstice.ShouldBe(P.MeanDayLengthHours + P.DayLengthAmplitudeHours, 1e-9);

    [Fact] public void Should_never_produce_a_day_shorter_than_nothing() => _shortest.ShouldBeGreaterThan(0.0);
    [Fact] public void Should_never_produce_a_day_longer_than_twenty_four_hours() => _longest.ShouldBeLessThan(24.0);
    [Fact] public void Should_keep_sunrise_and_sunset_symmetric_about_noon() => _sunrisePlusSunset.ShouldBe(24.0, 1e-9);
}
