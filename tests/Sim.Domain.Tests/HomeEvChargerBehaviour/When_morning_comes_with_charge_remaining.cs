using Shouldly;
using static Sim.Domain.Tests.HomeEvChargerBehaviourScenario.EvScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.HomeEvChargerBehaviourScenario;

/// <summary>
/// A-004: the car charges through the night, departure at 07:00 wipes whatever
/// remains, and the next evening starts a fresh session - the unfinished one is
/// forgotten, not resumed.
/// </summary>
public class When_morning_comes_with_charge_remaining
{
    private readonly double _throughTheNightKw;
    private readonly double _atDepartureKw;
    private readonly double _theNextEveningKw;

    public When_morning_comes_with_charge_remaining()
    {
        var behaviour = Behaviour();
        behaviour.PowerAt(Charger, TickAt(0, 18.0));
        _throughTheNightKw = behaviour.PowerAt(Charger, TickAt(1, 5.0)).Value;
        _atDepartureKw = behaviour.PowerAt(Charger, TickAt(1, 7.0)).Value;
        _theNextEveningKw = behaviour.PowerAt(Charger, TickAt(1, 18.0)).Value;
    }

    [Fact] public void Should_keep_charging_through_the_night() => _throughTheNightKw.ShouldBe(7.4, AbsoluteTolerance);
    [Fact] public void Should_go_silent_at_departure() => _atDepartureKw.ShouldBe(0, AbsoluteTolerance);
    [Fact] public void Should_start_a_fresh_session_the_next_evening() => _theNextEveningKw.ShouldBe(7.4, AbsoluteTolerance);
}
