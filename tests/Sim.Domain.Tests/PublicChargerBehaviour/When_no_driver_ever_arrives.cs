using Shouldly;
using static Sim.Domain.Tests.PublicChargerBehaviourScenario.PublicScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.PublicChargerBehaviourScenario;

/// <summary>A-004: no arrival, no session - the point meters nothing and never reports busy.</summary>
public class When_no_driver_ever_arrives
{
    private readonly double _totalMeteredKw;
    private readonly bool _everBusy;

    public When_no_driver_ever_arrives()
    {
        var behaviour = BehaviourWith(NeverAnyone);
        for (var slot = 0; slot < 8; slot++)
        {
            _totalMeteredKw += behaviour.PowerAt(Point, TickAt(slot * 0.25, slot)).Value;
            _everBusy |= behaviour.Busy;
        }
    }

    [Fact] public void Should_meter_nothing_at_all() => _totalMeteredKw.ShouldBe(0, AbsoluteTolerance);
    [Fact] public void Should_never_report_busy() => _everBusy.ShouldBeFalse();
}
