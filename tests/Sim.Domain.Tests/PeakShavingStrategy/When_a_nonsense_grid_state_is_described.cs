using Shouldly;
using Sim.Control.Domain;
using Sim.SharedKernel;

namespace Sim.Domain.Tests.PeakShavingStrategyScenario;

/// <summary>
/// TASK-024: the controller decides from this record alone, so a nonsense state
/// must be unrepresentable - born valid or not born.
/// </summary>
public class When_a_nonsense_grid_state_is_described
{
    private static Exception? Refusal(double soc, double capacity, double maxPower) =>
        Record.Exception(() => new GridState(new Kilowatts(10), soc, capacity, maxPower));

    private readonly Exception? _negativeCapacity = Refusal(soc: 10, capacity: -100, maxPower: 50);
    private readonly Exception? _zeroCapacity = Refusal(soc: 0, capacity: 0, maxPower: 50);
    private readonly Exception? _negativeMaxPower = Refusal(soc: 10, capacity: 100, maxPower: -50);
    private readonly Exception? _negativeCharge = Refusal(soc: -1, capacity: 100, maxPower: 50);
    private readonly Exception? _chargeBeyondTheCells = Refusal(soc: 101, capacity: 100, maxPower: 50);

    [Fact] public void Should_refuse_a_negative_capacity() => _negativeCapacity.ShouldBeOfType<ControlInvariantViolation>();
    [Fact] public void Should_refuse_a_capacity_of_nothing() => _zeroCapacity.ShouldBeOfType<ControlInvariantViolation>();
    [Fact] public void Should_refuse_a_negative_power_rating() => _negativeMaxPower.ShouldBeOfType<ControlInvariantViolation>();
    [Fact] public void Should_refuse_a_negative_state_of_charge() => _negativeCharge.ShouldBeOfType<ControlInvariantViolation>();
    [Fact] public void Should_refuse_more_charge_than_the_cells_hold() => _chargeBeyondTheCells.ShouldBeOfType<ControlInvariantViolation>();
}
