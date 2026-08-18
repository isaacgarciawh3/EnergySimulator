using Shouldly;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>
/// R-45 (round-trip efficiency): empty to full to empty again loses exactly the
/// configured round trip. Scenario: a 90 percent battery is drained, filled and
/// drained once more.
/// </summary>
public class When_a_full_round_trip_is_performed
{
    private readonly double _energyInKwh;
    private readonly double _energyOutKwh;

    public When_a_full_round_trip_is_performed()
    {
        var battery = Fresh();
        DrainCompletely(battery);
        _energyInKwh = FillCompletely(battery);
        _energyOutKwh = DrainCompletely(battery);
    }

    [Fact]
    public void Should_return_less_energy_than_it_took_in() =>
        _energyOutKwh.ShouldBeLessThan(_energyInKwh);

    [Fact]
    public void Should_lose_exactly_the_round_trip_efficiency() =>
        (_energyOutKwh / _energyInKwh).ShouldBe(RoundTrip, Close(_energyOutKwh / _energyInKwh, RoundTrip));
}
