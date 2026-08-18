using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.TemperatureModelScenario;

/// <summary>R-16 groundwork: the coldest day sits the full amplitude below the annual mean, and midsummer beats midwinter.</summary>
public class When_the_seasonal_mean_is_read
{
    private static readonly WeatherParameters P = WeatherParameters.Default;

    private readonly double _onTheColdestDay = Sim.Simulation.Domain.Weather.TemperatureModel.AverageTheSeasonalTemperatureC(P.ColdestDayOfYear, P);
    private readonly double _atMidsummer = Sim.Simulation.Domain.Weather.TemperatureModel.AverageTheSeasonalTemperatureC(196, P);
    private readonly double _atMidwinter = Sim.Simulation.Domain.Weather.TemperatureModel.AverageTheSeasonalTemperatureC(15, P);

    [Fact]
    public void Should_put_the_coldest_day_a_full_amplitude_below_the_mean() =>
        _onTheColdestDay.ShouldBe(P.AnnualMeanC - P.AnnualAmplitudeC, 1e-9);

    [Fact] public void Should_make_midsummer_warmer_than_midwinter() => _atMidsummer.ShouldBeGreaterThan(_atMidwinter);
}
