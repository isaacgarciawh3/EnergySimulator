using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>
/// CHARACTERISATION of a real model limitation, recorded as a running test
/// instead of a comment nobody executes: the clear-sky bell peaks at 1.0 at
/// midday in EVERY season, so a single-instant midday comparison is decided by
/// seeded cloud noise alone and flips for some seeds. If the model ever gains a
/// seasonal peak-irradiance term, Should_flip_for_some_seed fails - and that
/// failure is the signal to delete this scenario.
/// </summary>
public class When_only_midday_is_compared_across_seasons
{
    private readonly bool _configuredSeedFavoursSummer;
    private readonly int _seedsThatFlip;
    private readonly int _wholeDayFlips;

    public When_only_midday_is_compared_across_seasons()
    {
        _configuredSeedFavoursSummer =
            new WeatherModel(ConfiguredSeed).At(SummerAt(12)).IrradianceFactor
            > new WeatherModel(ConfiguredSeed).At(WinterAt(12)).IrradianceFactor;

        _seedsThatFlip = Seeds.Count(seed =>
        {
            var model = new WeatherModel(seed);
            return model.At(SummerAt(12)).IrradianceFactor <= model.At(WinterAt(12)).IrradianceFactor;
        });

        _wholeDayFlips = Seeds.Count(seed =>
        {
            var model = new WeatherModel(seed);
            return DailyIrradiance(model, SummerAt) <= DailyIrradiance(model, WinterAt);
        });
    }

    [Fact] public void Should_favour_summer_for_the_configured_seed() => _configuredSeedFavoursSummer.ShouldBeTrue();
    [Fact] public void Should_flip_for_some_seed_which_is_the_recorded_limitation() => _seedsThatFlip.ShouldBeGreaterThan(0);
    [Fact] public void Should_never_flip_when_the_whole_day_is_integrated() => _wholeDayFlips.ShouldBe(0);
}
