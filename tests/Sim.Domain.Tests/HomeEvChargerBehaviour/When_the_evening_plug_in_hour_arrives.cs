using Shouldly;
using static Sim.Domain.Tests.HomeEvChargerBehaviourScenario.EvScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.HomeEvChargerBehaviourScenario;

/// <summary>A-004: one plug-in per day inside the window - before the hour, silence; at the hour, rated power.</summary>
public class When_the_evening_plug_in_hour_arrives
{
    private readonly double _beforeTheHourKw;
    private readonly double _atTheHourKw;

    public When_the_evening_plug_in_hour_arrives()
    {
        var behaviour = Behaviour();
        _beforeTheHourKw = behaviour.PowerAt(Charger, TickAt(0, 17.75)).Value;
        _atTheHourKw = behaviour.PowerAt(Charger, TickAt(0, 18.0)).Value;
    }

    [Fact] public void Should_wait_in_silence_until_the_plug_in_hour() => _beforeTheHourKw.ShouldBe(0, AbsoluteTolerance);
    [Fact] public void Should_charge_at_the_rated_power_once_plugged() => _atTheHourKw.ShouldBe(7.4, AbsoluteTolerance);
}
