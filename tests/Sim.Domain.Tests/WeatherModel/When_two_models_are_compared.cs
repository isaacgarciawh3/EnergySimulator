using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>ADR-0006: the seed is the whole identity of a climate - same seed same sky, different seed different sky.</summary>
public class When_two_models_are_compared
{
    private readonly WeatherConditions _sameSeedFirst;
    private readonly WeatherConditions _sameSeedSecond;
    private readonly WeatherConditions _seedOne;
    private readonly WeatherConditions _seedTwo;

    public When_two_models_are_compared()
    {
        var noon = SummerAt(12);
        _sameSeedFirst = new WeatherModel(ConfiguredSeed).At(noon);
        _sameSeedSecond = new WeatherModel(ConfiguredSeed).At(noon);
        _seedOne = new WeatherModel(1).At(noon);
        _seedTwo = new WeatherModel(2).At(noon);
    }

    [Fact] public void Should_make_models_with_the_same_seed_agree_exactly() => _sameSeedSecond.ShouldBe(_sameSeedFirst);
    [Fact] public void Should_make_models_with_different_seeds_disagree() => _seedTwo.ShouldNotBe(_seedOne);
}
