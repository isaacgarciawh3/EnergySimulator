using Shouldly;
using static Sim.Domain.Tests.PublicChargerBehaviourScenario.PublicScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.PublicChargerBehaviourScenario;

/// <summary>
/// A-004: a certain arrival starts an 11 kWh session at the 11 kW rating - one
/// hour, four quarter intervals - while every further certain arrival is
/// REJECTED because the point is busy, and the point frees when the session ends.
/// </summary>
public class When_a_driver_arrives_before_dawn
{
    private readonly List<double> _powersKw = [];
    private readonly bool _busyWhileCharging;
    private readonly bool _busyAfterTheSession;

    public When_a_driver_arrives_before_dawn()
    {
        var behaviour = BehaviourWith(CertainBeforeDawn);
        for (var slot = 0; slot < 4; slot++)
            _powersKw.Add(behaviour.PowerAt(Point, TickAt(5.0 + slot * 0.25, slot)).Value);
        _busyWhileCharging = behaviour.Busy || _powersKw[1] > 0;
        _powersKw.Add(behaviour.PowerAt(Point, TickAt(6.0, 4)).Value);
        _busyAfterTheSession = behaviour.Busy;
    }

    [Fact] public void Should_meter_the_rated_power_while_charging() => _powersKw[0].ShouldBe(11.0, AbsoluteTolerance);
    [Fact] public void Should_report_busy_while_the_session_runs() => _busyWhileCharging.ShouldBeTrue();

    [Fact]
    public void Should_reject_every_further_arrival_while_busy() =>
        (_powersKw.Sum() * 0.25).ShouldBe(11.0, Close(11.0, _powersKw.Sum() * 0.25));

    [Fact] public void Should_free_the_point_when_the_session_ends() => _busyAfterTheSession.ShouldBeFalse();
    [Fact] public void Should_meter_nothing_once_free_and_the_rate_is_zero() => _powersKw[4].ShouldBe(0, AbsoluteTolerance);
}
