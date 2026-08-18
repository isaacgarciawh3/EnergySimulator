using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SmoothNoiseScenario;

/// <summary>A correlation period that does not run forwards is meaningless and must be refused.</summary>
public class When_a_non_positive_period_is_supplied
{
    private readonly Exception? _refusal =
        Record.Exception(() => Sim.Simulation.Domain.Weather.SmoothNoise.Locate(DateTimeOffset.UnixEpoch, TimeSpan.Zero));

    [Fact] public void Should_refuse_to_locate_anything() => _refusal.ShouldBeOfType<ArgumentOutOfRangeException>();
}
