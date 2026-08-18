using Shouldly;
using Sim.Simulation.Domain;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.WeatherParametersScenario;

/// <summary>
/// A nonsense climate must fail LOUDLY at construction, never quietly produce a
/// plausible-looking simulation. Scenario: every way a climate can be nonsense,
/// each captured in the constructor, each named by the rule it breaks.
/// </summary>
public class When_nonsense_climates_are_supplied
{
    private static readonly WeatherParameters Valid = WeatherParameters.Default;

    private static Exception? Refusal(WeatherParameters p) => Record.Exception(p.Validate);

    private readonly Exception? _swingWiderThanTheMean = Refusal(Valid with { DayLengthAmplitudeHours = 13.0 });
    private readonly Exception? _dayLongerThanTwentyFourHours = Refusal(Valid with { MeanDayLengthHours = 20.0, DayLengthAmplitudeHours = 5.0 });
    private readonly Exception? _coldestDayZero = Refusal(Valid with { ColdestDayOfYear = 0 });
    private readonly Exception? _coldestDayPastTheYear = Refusal(Valid with { ColdestDayOfYear = 366 });
    private readonly Exception? _flatClearSky = Refusal(Valid with { ClearSkyExponent = 0 });
    private readonly Exception? _cloudDarkerThanTotal = Refusal(Valid with { CloudAttenuation = 1.5 });
    private readonly Exception? _instantaneousNoise = Refusal(Valid with { NoiseCorrelationHours = 0 });
    private readonly Exception? _negativeAnnualSwing = Refusal(Valid with { AnnualAmplitudeC = -1 });
    private readonly Exception? _negativeDailySwing = Refusal(Valid with { DiurnalAmplitudeC = -1 });
    private readonly Exception? _negativeNoiseSwing = Refusal(Valid with { NoiseAmplitudeC = -1 });
    private readonly Exception? _longestDayPastTheYear = Refusal(Valid with { LongestDayOfYear = 366 });
    private readonly Exception? _coldestHourPastTheDay = Refusal(Valid with { ColdestHourOfDay = 24 });
    private readonly Exception? _cloudNoiseAboveOne = Refusal(Valid with { CloudNoiseScale = 1.5 });
    private readonly Exception? _winterBiasBeyondTotal = Refusal(Valid with { WinterCloudBias = 1.5 });
    private readonly Exception? _negativeDayLengthSwing = Refusal(Valid with { DayLengthAmplitudeHours = -1 });
    private readonly Exception? _longestDayBeforeTheYear = Refusal(Valid with { LongestDayOfYear = 0 });
    private readonly Exception? _coldestHourBeforeTheDay = Refusal(Valid with { ColdestHourOfDay = -1 });
    private readonly Exception? _negativeCloudNoise = Refusal(Valid with { CloudNoiseScale = -0.1 });
    private readonly Exception? _negativeCloudAttenuation = Refusal(Valid with { CloudAttenuation = -0.1 });

    [Fact] public void Should_refuse_a_day_length_swing_wider_than_the_mean_or_the_shortest_day_has_no_daylight() => _swingWiderThanTheMean.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_longest_day_beyond_twenty_four_hours() => _dayLongerThanTwentyFourHours.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_coldest_day_before_the_year_starts() => _coldestDayZero.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_coldest_day_after_the_year_ends() => _coldestDayPastTheYear.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_clear_sky_exponent_that_flattens_the_sun() => _flatClearSky.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_cloud_attenuation_that_could_drive_irradiance_negative() => _cloudDarkerThanTotal.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_noise_with_no_correlation_period() => _instantaneousNoise.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_negative_annual_temperature_swing() => _negativeAnnualSwing.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_negative_daily_temperature_swing() => _negativeDailySwing.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_negative_noise_swing() => _negativeNoiseSwing.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_longest_day_after_the_year_ends() => _longestDayPastTheYear.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_coldest_hour_past_the_day() => _coldestHourPastTheDay.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_cloud_noise_scaled_beyond_one() => _cloudNoiseAboveOne.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_winter_bias_beyond_total_cloud() => _winterBiasBeyondTotal.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_negative_day_length_swing() => _negativeDayLengthSwing.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_longest_day_before_the_year_starts() => _longestDayBeforeTheYear.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_a_coldest_hour_before_the_day_starts() => _coldestHourBeforeTheDay.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_negative_cloud_noise() => _negativeCloudNoise.ShouldBeOfType<SimulationInvariantViolation>();
    [Fact] public void Should_refuse_negative_cloud_attenuation() => _negativeCloudAttenuation.ShouldBeOfType<SimulationInvariantViolation>();
}
