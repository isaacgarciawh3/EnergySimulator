using Shouldly;
using Sim.Simulation.Domain;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests;

/// <summary>
/// Each rule of the weather model is tested on its own. Before the refactor
/// these behaviours were fifteen literals inside one method and none of them
/// could be reached without running the whole simulation.
/// </summary>
public class WeatherParametersTests
{
    private static WeatherParameters Valid => WeatherParameters.Default;

    [Fact]
    public void Default_parameters_are_valid() =>
        Should.NotThrow(() => Valid.Validate());

    [Fact]
    public void Day_length_swing_wider_than_the_mean_is_rejected_because_the_shortest_day_would_have_no_daylight() =>
        Should.Throw<ArgumentException>(() => (Valid with { DayLengthAmplitudeHours = 13.0 }).Validate());

    [Fact]
    public void Day_length_swing_that_would_exceed_24_hours_is_rejected() =>
        Should.Throw<ArgumentException>(() => (Valid with { MeanDayLengthHours = 20.0, DayLengthAmplitudeHours = 5.0 }).Validate());

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Coldest_day_outside_the_year_is_rejected(int day) =>
        Should.Throw<ArgumentException>(() => (Valid with { ColdestDayOfYear = day }).Validate());

    [Fact]
    public void Negative_clear_sky_exponent_is_rejected() =>
        Should.Throw<ArgumentException>(() => (Valid with { ClearSkyExponent = 0 }).Validate());

    [Fact]
    public void Cloud_attenuation_above_one_is_rejected_because_irradiance_could_go_negative() =>
        Should.Throw<ArgumentException>(() => (Valid with { CloudAttenuation = 1.5 }).Validate());

    [Fact]
    public void Non_positive_noise_correlation_is_rejected() =>
        Should.Throw<ArgumentException>(() => (Valid with { NoiseCorrelationHours = 0 }).Validate());

    [Fact]
    public void Invalid_parameters_fail_when_the_model_is_constructed_not_later() =>
        Should.Throw<ArgumentException>(() => new WeatherModel(1, Valid with { ClearSkyExponent = -1 }));
}

public class AnnualCycleTests
{
    [Fact]
    public void Peaks_at_one_on_its_peak_day() =>
        AnnualCycle.At(172, 172).ShouldBe(1.0, 1e-12);

    [Fact]
    public void Troughs_at_minus_one_half_a_year_from_its_peak() =>
        AnnualCycle.At(172 + 182, 172).ShouldBe(-1.0, 1e-3);

    [Fact]
    public void Stays_within_minus_one_and_one_for_every_day_of_the_year()
    {
        for (var day = 1; day <= 365; day++)
            AnnualCycle.At(day, 15).ShouldBeInRange(-1.0, 1.0);
    }
}

public class TemperatureModelTests
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    [Fact]
    public void Coldest_day_of_the_year_is_the_annual_mean_minus_the_amplitude() =>
        TemperatureModel.SeasonalMeanC(P.ColdestDayOfYear, P)
            .ShouldBe(P.AnnualMeanC - P.AnnualAmplitudeC, 1e-9);

    [Fact]
    public void Midsummer_is_warmer_than_midwinter() =>
        TemperatureModel.SeasonalMeanC(196, P)
            .ShouldBeGreaterThan(TemperatureModel.SeasonalMeanC(15, P));

    [Fact]
    public void Coldest_hour_of_the_day_has_the_lowest_diurnal_offset()
    {
        var atColdest = TemperatureModel.DiurnalOffsetC(P.ColdestHourOfDay, P);
        for (var hour = 0.0; hour < 24.0; hour += 0.5)
            TemperatureModel.DiurnalOffsetC(hour, P).ShouldBeGreaterThanOrEqualTo(atColdest - 1e-9);
    }

    [Fact]
    public void Afternoon_is_warmer_than_before_dawn() =>
        TemperatureModel.DiurnalOffsetC(15.0, P).ShouldBeGreaterThan(TemperatureModel.DiurnalOffsetC(3.0, P));

    [Fact]
    public void Noise_is_centred_so_that_a_mid_sample_shifts_nothing() =>
        TemperatureModel.NoiseOffsetC(0.5, P).ShouldBe(0.0, 1e-12);

    [Fact]
    public void Noise_never_exceeds_half_the_amplitude_in_either_direction()
    {
        TemperatureModel.NoiseOffsetC(0.0, P).ShouldBe(-P.NoiseAmplitudeC / 2, 1e-12);
        TemperatureModel.NoiseOffsetC(1.0, P).ShouldBe(P.NoiseAmplitudeC / 2, 1e-12);
    }
}

public class CloudModelTests
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Cover_is_always_a_fraction_between_zero_and_one(double noise)
    {
        for (var day = 1; day <= 365; day += 7)
            CloudModel.CoverFraction(day, noise, P).ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Winter_is_cloudier_than_summer_for_the_same_noise() =>
        CloudModel.CoverFraction(15, 0.5, P)
            .ShouldBeGreaterThan(CloudModel.CoverFraction(196, 0.5, P));
}

public class SolarGeometryTests
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    [Fact]
    public void Longest_day_is_at_the_summer_solstice() =>
        SolarGeometry.DayLengthHours(P.LongestDayOfYear, P)
            .ShouldBe(P.MeanDayLengthHours + P.DayLengthAmplitudeHours, 1e-9);

    [Fact]
    public void Day_length_is_always_a_sensible_number_of_hours()
    {
        for (var day = 1; day <= 365; day++)
            SolarGeometry.DayLengthHours(day, P).ShouldBeInRange(0.0, 24.0);
    }

    [Fact]
    public void Sunrise_and_sunset_are_symmetric_about_solar_noon()
    {
        var length = SolarGeometry.DayLengthHours(100, P);
        (SolarGeometry.SunriseHour(length) + SolarGeometry.SunsetHour(length)).ShouldBe(24.0, 1e-9);
    }

    [Fact]
    public void There_is_no_sun_before_sunrise_or_after_sunset()
    {
        SolarGeometry.ClearSkyFactor(2.0, 172, P).ShouldBe(0.0);
        SolarGeometry.ClearSkyFactor(23.5, 172, P).ShouldBe(0.0);
    }

    [Fact]
    public void Clear_sky_peaks_at_solar_noon() =>
        SolarGeometry.ClearSkyFactor(12.0, 172, P).ShouldBe(1.0, 1e-9);

    [Fact]
    public void Clear_sky_is_always_a_fraction_between_zero_and_one()
    {
        for (var day = 1; day <= 365; day += 11)
            for (var hour = 0.0; hour < 24.0; hour += 0.25)
                SolarGeometry.ClearSkyFactor(hour, day, P).ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Cloud_reduces_irradiance_but_never_below_zero()
    {
        SolarGeometry.IrradianceFactor(1.0, 0.0, P).ShouldBe(1.0, 1e-9);
        SolarGeometry.IrradianceFactor(1.0, 1.0, P).ShouldBe(1.0 - P.CloudAttenuation, 1e-9);
        SolarGeometry.IrradianceFactor(1.0, 1.0, P).ShouldBeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void Summer_midday_gets_more_sun_than_winter_midday() =>
        SolarGeometry.ClearSkyFactor(12.0, 172, P)
            .ShouldBeGreaterThanOrEqualTo(SolarGeometry.ClearSkyFactor(12.0, 15, P));
}

public class SmoothNoiseTests
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(3);

    [Fact]
    public void Fraction_is_zero_exactly_on_a_block_boundary()
    {
        var epoch = DateTimeOffset.FromUnixTimeSeconds(0);
        SmoothNoise.Locate(epoch, Period).Fraction.ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void Fraction_is_always_within_zero_and_one_including_before_the_epoch()
    {
        var start = new DateTimeOffset(1960, 3, 4, 5, 6, 7, TimeSpan.Zero);
        for (var i = 0; i < 500; i++)
            SmoothNoise.Locate(start.AddMinutes(i * 7), Period).Fraction.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Blend_returns_the_endpoints_and_the_midpoint()
    {
        SmoothNoise.Blend(2, 6, 0).ShouldBe(2);
        SmoothNoise.Blend(2, 6, 1).ShouldBe(6);
        SmoothNoise.Blend(2, 6, 0.5).ShouldBe(4);
    }

    [Fact]
    public void A_non_positive_correlation_period_is_rejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => SmoothNoise.Locate(DateTimeOffset.UtcNow, TimeSpan.Zero));

    [Fact]
    public void Output_is_always_within_zero_and_one()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 400; i++)
            SmoothNoise.At(42, 7, start.AddMinutes(i * 13), Period).ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Signal_is_continuous_across_a_block_boundary()
    {
        var boundary = DateTimeOffset.FromUnixTimeSeconds(3 * 3600 * 100);
        var before = SmoothNoise.At(42, 7, boundary.AddSeconds(-1), Period);
        var after = SmoothNoise.At(42, 7, boundary.AddSeconds(1), Period);
        Math.Abs(after - before).ShouldBeLessThan(0.01);
    }

    [Fact]
    public void Same_inputs_always_give_the_same_value()
    {
        var t = new DateTimeOffset(2026, 5, 5, 5, 5, 0, TimeSpan.Zero);
        SmoothNoise.At(9, 3, t, Period).ShouldBe(SmoothNoise.At(9, 3, t, Period));
    }
}

public class WeatherModelCompositionTests
{
    private static WeatherModel Model => new(20260818);

    [Fact]
    public void Weather_is_a_pure_function_of_instant_and_seed_so_the_clock_can_jump()
    {
        var t = new DateTimeOffset(2026, 7, 1, 13, 0, 0, TimeSpan.Zero);
        Model.At(t).ShouldBe(Model.At(t));
    }

    [Fact]
    public void There_is_no_irradiance_at_night() =>
        Model.At(new DateTimeOffset(2026, 6, 21, 2, 0, 0, TimeSpan.Zero)).IrradianceFactor.ShouldBe(0.0);

    [Fact]
    public void Summer_midday_produces_more_irradiance_than_winter_midday()
    {
        var summer = Model.At(new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero)).IrradianceFactor;
        var winter = Model.At(new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.Zero)).IrradianceFactor;
        summer.ShouldBeGreaterThan(winter);
    }

    [Theory]
    [InlineData(1, Season.Winter)]
    [InlineData(4, Season.Spring)]
    [InlineData(7, Season.Summer)]
    [InlineData(10, Season.Autumn)]
    public void Season_follows_the_month(int month, Season expected) =>
        Model.At(new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero)).Season.ShouldBe(expected);

    [Fact]
    public void Reported_values_always_stay_within_their_physical_ranges()
    {
        var t = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 400; i++)
        {
            var w = Model.At(t.AddHours(i * 2.3));
            w.CloudCover.ShouldBeInRange(0.0, 1.0);
            w.IrradianceFactor.ShouldBeInRange(0.0, 1.0);
            w.TemperatureC.ShouldBeInRange(-40.0, 50.0);
        }
    }
}
