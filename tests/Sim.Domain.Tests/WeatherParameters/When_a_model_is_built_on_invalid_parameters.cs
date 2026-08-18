using Shouldly;
using Sim.Simulation.Domain;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.WeatherParametersScenario;

/// <summary>Validation happens when the model is CONSTRUCTED, not on first use three ticks later.</summary>
public class When_a_model_is_built_on_invalid_parameters
{
    private readonly Exception? _refusal =
        Record.Exception(() => new WeatherModel(1, WeatherParameters.Default with { ClearSkyExponent = -1 }));

    [Fact] public void Should_fail_at_construction() => _refusal.ShouldBeOfType<SimulationInvariantViolation>();
}
