using Shouldly;
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

    [Fact] public void Should_refuse_a_day_length_swing_wider_than_the_mean_or_the_shortest_day_has_no_daylight() => _swingWiderThanTheMean.ShouldBeOfType<ArgumentException>();
    [Fact] public void Should_refuse_a_longest_day_beyond_twenty_four_hours() => _dayLongerThanTwentyFourHours.ShouldBeOfType<ArgumentException>();
    [Fact] public void Should_refuse_a_coldest_day_before_the_year_starts() => _coldestDayZero.ShouldBeOfType<ArgumentException>();
    [Fact] public void Should_refuse_a_coldest_day_after_the_year_ends() => _coldestDayPastTheYear.ShouldBeOfType<ArgumentException>();
    [Fact] public void Should_refuse_a_clear_sky_exponent_that_flattens_the_sun() => _flatClearSky.ShouldBeOfType<ArgumentException>();
    [Fact] public void Should_refuse_cloud_attenuation_that_could_drive_irradiance_negative() => _cloudDarkerThanTotal.ShouldBeOfType<ArgumentException>();
    [Fact] public void Should_refuse_noise_with_no_correlation_period() => _instantaneousNoise.ShouldBeOfType<ArgumentException>();
}
