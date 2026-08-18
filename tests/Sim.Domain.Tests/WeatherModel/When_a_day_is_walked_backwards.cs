using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>
/// ADR-0006: a model that accumulated state would disagree when asked out of
/// order; a pure function cannot. Scenario: the same day sampled forwards and
/// then backwards through one model instance.
/// </summary>
public class When_a_day_is_walked_backwards
{
    private readonly List<WeatherConditions> _forwards;
    private readonly List<WeatherConditions> _backwards;

    public When_a_day_is_walked_backwards()
    {
        var hours = new[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0 };
        _forwards = hours.Select(h => new WeatherModel(ConfiguredSeed).At(SummerAt(h))).ToList();

        var walkedBackwards = new WeatherModel(ConfiguredSeed);
        _backwards = [];
        for (var i = hours.Length - 1; i >= 0; i--) _backwards.Add(walkedBackwards.At(SummerAt(hours[i])));
        _backwards.Reverse();
    }

    [Fact] public void Should_see_the_same_weather_as_walking_forwards() => _backwards.ShouldBe(_forwards);
}
