using Shouldly;
using static Sim.Domain.Tests.HomeEvChargerBehaviourScenario.EvScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.HomeEvChargerBehaviourScenario;

/// <summary>A-004: ONE plug-in per day - a completed session does not restart however long the evening runs.</summary>
public class When_the_evening_continues_after_the_session_ends
{
    private readonly double _laterThatEveningKw;

    public When_the_evening_continues_after_the_session_ends()
    {
        var behaviour = Behaviour();
        for (var slot = 0; slot < 7; slot++)
            behaviour.PowerAt(Charger, TickAt(0, 18.0 + slot * 0.25));
        _laterThatEveningKw = behaviour.PowerAt(Charger, TickAt(0, 22.0)).Value;
    }

    [Fact] public void Should_not_plug_in_twice_the_same_day() => _laterThatEveningKw.ShouldBe(0, AbsoluteTolerance);
}
