using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.TemperatureModelScenario;

/// <summary>Noise is centred: a middle sample shifts nothing, the extremes swing exactly half the amplitude each way.</summary>
public class When_the_noise_sample_moves_through_its_range
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _atTheCentre = Sim.Simulation.Domain.Weather.TemperatureModel.NoiseOffsetC(0.5, P);
    private readonly double _atTheBottom = Sim.Simulation.Domain.Weather.TemperatureModel.NoiseOffsetC(0.0, P);
    private readonly double _atTheTop = Sim.Simulation.Domain.Weather.TemperatureModel.NoiseOffsetC(1.0, P);

    [Fact] public void Should_shift_nothing_at_dead_centre() => _atTheCentre.ShouldBe(0.0, 1e-12);
    [Fact] public void Should_swing_down_half_the_amplitude_at_the_bottom() => _atTheBottom.ShouldBe(-P.NoiseAmplitudeC / 2, 1e-12);
    [Fact] public void Should_swing_up_half_the_amplitude_at_the_top() => _atTheTop.ShouldBe(P.NoiseAmplitudeC / 2, 1e-12);
}
