using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>R-16 (temperature drives the heat pump) and R-14 (season): January noon is colder than July noon, always.</summary>
public class When_winter_and_summer_noon_are_compared_for_any_seed
{
    private readonly double _widestWinterMinusSummer;
    private readonly bool _januaryIsAlwaysWinter = true;
    private readonly bool _julyIsAlwaysSummer = true;

    public When_winter_and_summer_noon_are_compared_for_any_seed()
    {
        _widestWinterMinusSummer = double.MinValue;
        foreach (var seed in Seeds)
        {
            var model = new WeatherModel(seed);
            var winter = model.At(WinterAt(12));
            var summer = model.At(SummerAt(12));

            _widestWinterMinusSummer = Math.Max(_widestWinterMinusSummer, winter.TemperatureC - summer.TemperatureC);
            if (winter.Season != Season.Winter) _januaryIsAlwaysWinter = false;
            if (summer.Season != Season.Summer) _julyIsAlwaysSummer = false;
        }
    }

    [Fact] public void Should_report_winter_colder_than_summer() => _widestWinterMinusSummer.ShouldBeLessThan(0);
    [Fact] public void Should_label_january_as_winter() => _januaryIsAlwaysWinter.ShouldBeTrue();
    [Fact] public void Should_label_july_as_summer() => _julyIsAlwaysSummer.ShouldBeTrue();
}
