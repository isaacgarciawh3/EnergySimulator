using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.WeatherModelScenario.WeatherScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>RF-15/R-15 boundary: the sun is down, so PV has nothing - in every season, for every seed.</summary>
public class When_night_falls_in_any_season
{
    private readonly double _highestNightIrradiance;

    public When_night_falls_in_any_season() =>
        _highestNightIrradiance = Seeds.Max(seed =>
        {
            var model = new WeatherModel(seed);
            return new[] { 0.0, 1.0, 2.0, 23.0 }
                .Max(h => Math.Max(model.At(SummerAt(h)).IrradianceFactor, model.At(WinterAt(h)).IrradianceFactor));
        });

    [Fact] public void Should_produce_no_irradiance_at_all() => _highestNightIrradiance.ShouldBe(0, AbsoluteTolerance);
}
