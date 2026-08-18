using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>
/// R-15, the form that holds UNCONDITIONALLY: seasonality lives in day length,
/// so a summer day out-produces a winter day for EVERY seed - observed margin
/// 2x to 4x, asserted at 1.5x as a floor, never a fitted constant.
/// </summary>
public class When_whole_days_are_integrated_across_seasons
{
    private readonly double _smallestSummerToWinterRatio;

    public When_whole_days_are_integrated_across_seasons() =>
        _smallestSummerToWinterRatio = Seeds.Min(seed =>
        {
            var model = new WeatherModel(seed);
            return DailyIrradiance(model, SummerAt) / DailyIrradiance(model, WinterAt);
        });

    [Fact]
    public void Should_yield_at_least_half_again_more_summer_solar_for_every_seed() =>
        _smallestSummerToWinterRatio.ShouldBeGreaterThan(1.5);
}
