using Shouldly;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>R-45: with efficiency 1.0 the loss term vanishes entirely - the boundary case of the loss model.</summary>
public class When_a_lossless_battery_performs_a_round_trip
{
    private readonly double _energyInKwh;
    private readonly double _energyOutKwh;

    public When_a_lossless_battery_performs_a_round_trip()
    {
        var battery = Fresh(roundTrip: 1.0);
        DrainCompletely(battery);
        _energyInKwh = FillCompletely(battery);
        _energyOutKwh = DrainCompletely(battery);
    }

    [Fact]
    public void Should_return_everything_it_took_in() =>
        _energyOutKwh.ShouldBe(_energyInKwh, Close(_energyInKwh, _energyOutKwh));
}
