using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>R-15 (weather drives PV): the summer midday sun always delivers something, whatever the seeded clouds do.</summary>
public class When_summer_midday_arrives_for_any_seed
{
    private readonly double _lowestMiddayIrradiance;

    public When_summer_midday_arrives_for_any_seed() =>
        _lowestMiddayIrradiance = Seeds.Min(seed => new WeatherModel(seed).At(SummerAt(12)).IrradianceFactor);

    [Fact] public void Should_always_deliver_positive_irradiance() => _lowestMiddayIrradiance.ShouldBeGreaterThan(0);
}
