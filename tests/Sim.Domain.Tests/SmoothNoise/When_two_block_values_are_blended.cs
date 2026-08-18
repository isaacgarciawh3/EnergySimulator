using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SmoothNoiseScenario;

/// <summary>How we interpolate is a decision, not an expression - so it is named, and proven at its three defining points.</summary>
public class When_two_block_values_are_blended
{
    private readonly double _atTheStart = Sim.Simulation.Domain.Weather.SmoothNoise.Blend(2, 6, 0);
    private readonly double _atTheEnd = Sim.Simulation.Domain.Weather.SmoothNoise.Blend(2, 6, 1);
    private readonly double _atTheMiddle = Sim.Simulation.Domain.Weather.SmoothNoise.Blend(2, 6, 0.5);

    [Fact] public void Should_return_the_first_value_at_the_start() => _atTheStart.ShouldBe(2);
    [Fact] public void Should_return_the_second_value_at_the_end() => _atTheEnd.ShouldBe(6);
    [Fact] public void Should_return_the_midpoint_halfway() => _atTheMiddle.ShouldBe(4);
}
