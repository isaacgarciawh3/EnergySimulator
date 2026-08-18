using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.SolarGeometryScenario;

/// <summary>R-15: cloud reduces what reaches the panels, bounded so irradiance can never go negative.</summary>
public class When_cloud_attenuates_the_sky
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _underAClearSky = Sim.Simulation.Domain.Weather.SolarGeometry.IrradianceFactor(1.0, 0.0, P);
    private readonly double _underTotalCloud = Sim.Simulation.Domain.Weather.SolarGeometry.IrradianceFactor(1.0, 1.0, P);

    [Fact] public void Should_pass_everything_through_a_clear_sky() => _underAClearSky.ShouldBe(1.0, 1e-9);
    [Fact] public void Should_attenuate_total_cloud_by_the_configured_factor() => _underTotalCloud.ShouldBe(1.0 - P.CloudAttenuation, 1e-9);
    [Fact] public void Should_never_drive_irradiance_negative() => _underTotalCloud.ShouldBeGreaterThanOrEqualTo(0.0);
}
