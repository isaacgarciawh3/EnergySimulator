using Shouldly;
using Sim.Application.Configuration;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests;

/// <summary>
/// Weather has to drive PV and the heat pump, and it has to be reproducible.
/// Both follow from the same design decision: weather is a PURE FUNCTION of
/// instant and seed rather than an accumulating random walk, so the clock can be
/// restarted or jumped forward and still produce the same day.
///
/// Every claim here is checked against a SWEEP OF SEEDS rather than one lucky
/// one. That matters more than it sounds: the single-seed version of the
/// midday seasonality check passes by coincidence and flips for other seeds,
/// which is what <see cref="Midday_irradiance_alone_is_not_a_reliable_seasonality_signal"/>
/// records.
/// </summary>
public sealed class WeatherSeasonalityTests
{
    private static readonly ulong ConfiguredSeed = unchecked((ulong)SimulationConfiguration.Default.Seed);

    private static readonly ulong[] Seeds = [20260818, 1, 2, 3, 7, 42, 123, 999, 555, 31337, 2026, 88];

    private static DateTimeOffset SummerAt(double hour) =>
        new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromHours(hour);

    private static DateTimeOffset WinterAt(double hour) =>
        new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromHours(hour);

    // 19
    [Fact]
    public void Weather_is_a_pure_function_of_instant_and_seed()
    {
        var model = new WeatherModel(ConfiguredSeed);

        foreach (var hour in new[] { 0.0, 2.0, 6.25, 12.0, 17.75, 23.5 })
        {
            var first = model.At(SummerAt(hour));
            var second = model.At(SummerAt(hour));

            // Exact, no tolerance: the claim is purity, not approximate agreement.
            second.ShouldBe(first);
        }
    }

    // 19
    [Fact]
    public void Two_models_with_the_same_seed_agree_and_a_different_seed_does_not()
    {
        var instant = SummerAt(12);

        new WeatherModel(ConfiguredSeed).At(instant).ShouldBe(new WeatherModel(ConfiguredSeed).At(instant));
        new WeatherModel(1).At(instant).ShouldNotBe(new WeatherModel(2).At(instant));
    }

    // 19
    [Fact]
    public void Weather_does_not_depend_on_the_order_instants_are_asked_for()
    {
        var hours = new[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0 };

        var forwards = hours.Select(h => new WeatherModel(ConfiguredSeed).At(SummerAt(h))).ToList();

        // The same model instance walked backwards through the day. A model that
        // accumulated state would disagree here; a pure function cannot.
        var walkedBackwards = new WeatherModel(ConfiguredSeed);
        var backwards = new List<WeatherConditions>();
        for (var i = hours.Length - 1; i >= 0; i--) backwards.Add(walkedBackwards.At(SummerAt(hours[i])));
        backwards.Reverse();

        backwards.ShouldBe(forwards);
    }

    // 20
    [Fact]
    public void Irradiance_is_zero_at_night_in_every_season_and_for_every_seed()
    {
        foreach (var seed in Seeds)
        {
            var model = new WeatherModel(seed);

            foreach (var hour in new[] { 0.0, 1.0, 2.0, 23.0 })
            {
                model.At(SummerAt(hour)).IrradianceFactor.ShouldBe(0, AbsoluteTolerance);
                model.At(WinterAt(hour)).IrradianceFactor.ShouldBe(0, AbsoluteTolerance);
            }
        }
    }

    // 20
    [Fact]
    public void Irradiance_is_positive_at_summer_midday_for_every_seed()
    {
        foreach (var seed in Seeds)
            new WeatherModel(seed).At(SummerAt(12)).IrradianceFactor.ShouldBeGreaterThan(0);
    }

    // 21
    [Fact]
    public void A_summer_day_yields_more_solar_energy_than_a_winter_day_for_every_seed()
    {
        foreach (var seed in Seeds)
        {
            var model = new WeatherModel(seed);

            var summer = DailyIrradiance(model, SummerAt);
            var winter = DailyIrradiance(model, WinterAt);

            // Twice the daylight and thinner cloud: observed margin is 2.0x to 4.0x,
            // so 1.5x is a floor with room to spare rather than a fitted constant.
            summer.ShouldBeGreaterThan(winter * 1.5);
        }
    }

    // 21
    [Fact]
    public void Summer_midday_irradiance_exceeds_winter_midday_irradiance_for_the_configured_seed()
    {
        var model = new WeatherModel(ConfiguredSeed);

        var summer = model.At(SummerAt(12)).IrradianceFactor;
        var winter = model.At(WinterAt(12)).IrradianceFactor;

        summer.ShouldBeGreaterThan(winter);
    }

    /// <summary>
    /// Characterisation test: it records a real limitation of the model instead of
    /// leaving it as a comment nobody runs.
    ///
    /// The clear-sky bell is centred on 12:00 in EVERY season, so its value at
    /// exactly midday is 1.0 all year and only cloud cover separates July from
    /// January. A single-instant midday comparison is therefore decided by seeded
    /// noise and flips for some seeds - 2 of the 12 swept here. Seasonality lives
    /// in DAY LENGTH, which is why the whole-day test above is the one that holds
    /// unconditionally. If the model ever gains a seasonal peak-irradiance term
    /// this test will fail, and that failure is the signal to delete it.
    /// </summary>
    // 21
    [Fact]
    public void Midday_irradiance_alone_is_not_a_reliable_seasonality_signal()
    {
        var flips = Seeds.Count(seed =>
        {
            var model = new WeatherModel(seed);
            return model.At(SummerAt(12)).IrradianceFactor <= model.At(WinterAt(12)).IrradianceFactor;
        });

        flips.ShouldBeGreaterThan(0);

        // Whereas the whole-day comparison never flips, for any seed swept.
        Seeds.Count(seed =>
        {
            var model = new WeatherModel(seed);
            return DailyIrradiance(model, SummerAt) <= DailyIrradiance(model, WinterAt);
        }).ShouldBe(0);
    }

    // 21
    [Fact]
    public void Winter_is_colder_than_summer_at_the_same_hour_for_every_seed()
    {
        foreach (var seed in Seeds)
        {
            var model = new WeatherModel(seed);

            model.At(WinterAt(12)).TemperatureC.ShouldBeLessThan(model.At(SummerAt(12)).TemperatureC);
            model.At(WinterAt(12)).Season.ShouldBe(Season.Winter);
            model.At(SummerAt(12)).Season.ShouldBe(Season.Summer);
        }
    }

    /// <summary>Irradiance integrated over one day at the default 15 minute interval.</summary>
    private static double DailyIrradiance(WeatherModel model, Func<double, DateTimeOffset> day) =>
        Enumerable.Range(0, 96).Sum(slot => model.At(day(slot / 4.0)).IrradianceFactor) * 0.25;
}
