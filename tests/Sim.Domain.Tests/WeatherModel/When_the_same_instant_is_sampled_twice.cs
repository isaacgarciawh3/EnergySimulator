using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>
/// ADR-0006: weather is a PURE FUNCTION of instant and seed, so the clock can
/// jump and still produce the same day. Exact equality - the claim is purity.
/// </summary>
public class When_the_same_instant_is_sampled_twice
{
    private readonly bool _everyHourAgreedExactly = true;

    public When_the_same_instant_is_sampled_twice()
    {
        var model = new WeatherModel(ConfiguredSeed);
        foreach (var hour in new[] { 0.0, 2.0, 6.25, 12.0, 17.75, 23.5 })
            if (!model.At(SummerAt(hour)).Equals(model.At(SummerAt(hour))))
                _everyHourAgreedExactly = false;
    }

    [Fact] public void Should_return_exactly_the_same_weather() => _everyHourAgreedExactly.ShouldBeTrue();
}
