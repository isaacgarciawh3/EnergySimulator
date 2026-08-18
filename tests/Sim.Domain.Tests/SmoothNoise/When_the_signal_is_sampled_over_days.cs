using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SmoothNoiseScenario;

/// <summary>
/// The whole point of value noise: always inside [0,1), continuous across block
/// boundaries, and identical for identical inputs.
/// </summary>
public class When_the_signal_is_sampled_over_days
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(3);

    private readonly double _lowest = double.MaxValue;
    private readonly double _highest = double.MinValue;
    private readonly double _jumpAcrossABoundary;
    private readonly double _firstSample;
    private readonly double _secondSample;

    public When_the_signal_is_sampled_over_days()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 400; i++)
        {
            var value = Sim.Simulation.Domain.Weather.SmoothNoise.At(42, 7, start.AddMinutes(i * 13), Period);
            _lowest = Math.Min(_lowest, value);
            _highest = Math.Max(_highest, value);
        }

        var boundary = DateTimeOffset.FromUnixTimeSeconds(3 * 3600 * 100);
        _jumpAcrossABoundary = Math.Abs(
            Sim.Simulation.Domain.Weather.SmoothNoise.At(42, 7, boundary.AddSeconds(1), Period)
            - Sim.Simulation.Domain.Weather.SmoothNoise.At(42, 7, boundary.AddSeconds(-1), Period));

        var instant = new DateTimeOffset(2026, 5, 5, 5, 5, 0, TimeSpan.Zero);
        _firstSample = Sim.Simulation.Domain.Weather.SmoothNoise.At(9, 3, instant, Period);
        _secondSample = Sim.Simulation.Domain.Weather.SmoothNoise.At(9, 3, instant, Period);
    }

    [Fact] public void Should_never_go_below_zero() => _lowest.ShouldBeGreaterThanOrEqualTo(0.0);
    [Fact] public void Should_stay_below_one() => _highest.ShouldBeLessThan(1.0);
    [Fact] public void Should_stay_continuous_across_a_block_boundary() => _jumpAcrossABoundary.ShouldBeLessThan(0.01);
    [Fact] public void Should_repeat_exactly_for_identical_inputs() => _secondSample.ShouldBe(_firstSample);
}
