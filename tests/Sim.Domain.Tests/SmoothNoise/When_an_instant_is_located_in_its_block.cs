using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SmoothNoiseScenario;

/// <summary>The block arithmetic: boundaries land on fraction zero, and instants before the epoch still land inside [0,1).</summary>
public class When_an_instant_is_located_in_its_block
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(3);

    private readonly double _fractionOnTheBoundary;
    private readonly double _lowestFractionBeforeTheEpoch = double.MaxValue;
    private readonly double _highestFractionBeforeTheEpoch = double.MinValue;

    public When_an_instant_is_located_in_its_block()
    {
        _fractionOnTheBoundary = Sim.Simulation.Domain.Weather.SmoothNoise
            .Locate(DateTimeOffset.FromUnixTimeSeconds(0), Period).Fraction;

        var start = new DateTimeOffset(1960, 3, 4, 5, 6, 7, TimeSpan.Zero);
        for (var i = 0; i < 500; i++)
        {
            var fraction = Sim.Simulation.Domain.Weather.SmoothNoise.Locate(start.AddMinutes(i * 7), Period).Fraction;
            _lowestFractionBeforeTheEpoch = Math.Min(_lowestFractionBeforeTheEpoch, fraction);
            _highestFractionBeforeTheEpoch = Math.Max(_highestFractionBeforeTheEpoch, fraction);
        }
    }

    [Fact] public void Should_land_exactly_on_zero_at_a_block_boundary() => _fractionOnTheBoundary.ShouldBe(0.0, 1e-12);
    [Fact] public void Should_never_go_negative_before_the_epoch() => _lowestFractionBeforeTheEpoch.ShouldBeGreaterThanOrEqualTo(0.0);
    [Fact] public void Should_stay_below_one_before_the_epoch() => _highestFractionBeforeTheEpoch.ShouldBeLessThan(1.0);
}
