using Shouldly;
using static Sim.Domain.Tests.HomeEvChargerBehaviourScenario.EvScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.HomeEvChargerBehaviourScenario;

/// <summary>
/// A-004: a 10 kWh session at 7.4 kW across quarter hours - five full intervals,
/// one partial reported as the interval AVERAGE, then silence, and the books
/// close on exactly the session energy.
/// </summary>
public class When_the_car_charges_to_completion
{
    private readonly List<double> _powersKw = [];

    public When_the_car_charges_to_completion()
    {
        var behaviour = Behaviour();
        for (var slot = 0; slot < 7; slot++)
            _powersKw.Add(behaviour.PowerAt(Charger, TickAt(0, 18.0 + slot * 0.25)).Value);
    }

    [Fact] public void Should_charge_at_full_power_while_energy_remains() => _powersKw[4].ShouldBe(7.4, AbsoluteTolerance);

    [Fact]
    public void Should_meter_the_interval_average_on_the_final_partial_interval() =>
        _powersKw[5].ShouldBe(3.0, Close(3.0, _powersKw[5]));

    [Fact] public void Should_meter_nothing_once_the_session_is_complete() => _powersKw[6].ShouldBe(0, AbsoluteTolerance);

    [Fact]
    public void Should_deliver_exactly_the_session_energy() =>
        (_powersKw.Sum() * 0.25).ShouldBe(10.0, Close(10.0, _powersKw.Sum() * 0.25));
}
