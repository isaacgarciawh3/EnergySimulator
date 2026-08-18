using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.TemperatureModelScenario;

/// <summary>The diurnal swing: coldest before dawn, warmest mid afternoon.</summary>
public class When_the_day_warms_and_cools
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _atTheColdestHour;
    private readonly double _lowestOfTheDay = double.MaxValue;
    private readonly double _midAfternoon;
    private readonly double _beforeDawn;

    public When_the_day_warms_and_cools()
    {
        _atTheColdestHour = Sim.Simulation.Domain.Weather.TemperatureModel.DiurnalOffsetC(P.ColdestHourOfDay, P);
        for (var hour = 0.0; hour < 24.0; hour += 0.5)
            _lowestOfTheDay = Math.Min(_lowestOfTheDay, Sim.Simulation.Domain.Weather.TemperatureModel.DiurnalOffsetC(hour, P));
        _midAfternoon = Sim.Simulation.Domain.Weather.TemperatureModel.DiurnalOffsetC(15.0, P);
        _beforeDawn = Sim.Simulation.Domain.Weather.TemperatureModel.DiurnalOffsetC(3.0, P);
    }

    [Fact]
    public void Should_make_the_configured_coldest_hour_the_minimum_of_the_day() =>
        _atTheColdestHour.ShouldBe(_lowestOfTheDay, 1e-9);

    [Fact] public void Should_make_the_afternoon_warmer_than_before_dawn() => _midAfternoon.ShouldBeGreaterThan(_beforeDawn);
}
