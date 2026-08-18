using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>R-14: the season follows the simulated month - one representative month per season.</summary>
public class When_each_season_month_is_sampled
{
    private static Season SeasonIn(int month) =>
        new WeatherModel(ConfiguredSeed).At(new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero)).Season;

    private readonly Season _january = SeasonIn(1);
    private readonly Season _april = SeasonIn(4);
    private readonly Season _july = SeasonIn(7);
    private readonly Season _october = SeasonIn(10);

    [Fact] public void Should_call_january_winter() => _january.ShouldBe(Season.Winter);
    [Fact] public void Should_call_april_spring() => _april.ShouldBe(Season.Spring);
    [Fact] public void Should_call_july_summer() => _july.ShouldBe(Season.Summer);
    [Fact] public void Should_call_october_autumn() => _october.ShouldBe(Season.Autumn);
}
