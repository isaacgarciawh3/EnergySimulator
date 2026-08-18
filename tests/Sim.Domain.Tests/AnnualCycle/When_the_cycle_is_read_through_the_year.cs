using Shouldly;
using Sim.Simulation.Domain.Weather;

namespace Sim.Domain.Tests.AnnualCycleScenario;

/// <summary>
/// The one cosine both temperature and day length are built from: peaks at
/// exactly one on its peak day, troughs half a year away, never leaves [-1, 1].
/// </summary>
public class When_the_cycle_is_read_through_the_year
{
    private readonly double _onThePeakDay;
    private readonly double _halfAYearAway;
    private readonly double _lowestOfTheYear = double.MaxValue;
    private readonly double _highestOfTheYear = double.MinValue;

    public When_the_cycle_is_read_through_the_year()
    {
        _onThePeakDay = Sim.Simulation.Domain.Weather.AnnualCycle.At(172, 172);
        _halfAYearAway = Sim.Simulation.Domain.Weather.AnnualCycle.At(172 + 182, 172);
        for (var day = 1; day <= 365; day++)
        {
            var value = Sim.Simulation.Domain.Weather.AnnualCycle.At(day, 15);
            _lowestOfTheYear = Math.Min(_lowestOfTheYear, value);
            _highestOfTheYear = Math.Max(_highestOfTheYear, value);
        }
    }

    [Fact] public void Should_peak_at_exactly_one_on_the_peak_day() => _onThePeakDay.ShouldBe(1.0, 1e-12);
    [Fact] public void Should_trough_at_minus_one_half_a_year_away() => _halfAYearAway.ShouldBe(-1.0, 1e-3);
    [Fact] public void Should_never_drop_below_minus_one() => _lowestOfTheYear.ShouldBeGreaterThanOrEqualTo(-1.0);
    [Fact] public void Should_never_rise_above_one() => _highestOfTheYear.ShouldBeLessThanOrEqualTo(1.0);
}
