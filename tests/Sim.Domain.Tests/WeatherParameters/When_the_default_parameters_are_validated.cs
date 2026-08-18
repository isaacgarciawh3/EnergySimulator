using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.WeatherParametersScenario;

/// <summary>The shipped climate must be valid, or the application cannot boot at all.</summary>
public class When_the_default_parameters_are_validated
{
    private readonly Exception? _refusal = Record.Exception(() => WeatherParameters.Default.Validate());

    [Fact] public void Should_pass_without_complaint() => _refusal.ShouldBeNull();
}
