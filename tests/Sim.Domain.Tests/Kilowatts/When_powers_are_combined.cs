using Shouldly;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.KilowattsScenario;

/// <summary>
/// ADR-0002: power is a TYPE, not a bare double - and its arithmetic is the
/// arithmetic of the sign convention. The units suffix in the text keeps a
/// human from ever mistaking a power for an energy.
/// </summary>
public class When_powers_are_combined
{
    private static readonly Kilowatts Two = new(2);
    private static readonly Kilowatts Three = new(3);

    [Fact] public void Should_add_as_plain_numbers() => (Two + Three).Value.ShouldBe(5, AbsoluteTolerance);
    [Fact] public void Should_subtract_keeping_the_sign() => (Two - Three).Value.ShouldBe(-1, AbsoluteTolerance);
    [Fact] public void Should_negate_consumption_into_generation() => (-Two).Value.ShouldBe(-2, AbsoluteTolerance);
    [Fact] public void Should_become_energy_only_through_a_duration() => Two.Over(TimeSpan.FromHours(1)).Value.ShouldBe(2, AbsoluteTolerance);
    [Fact] public void Should_order_smaller_before_larger() => Two.CompareTo(Three).ShouldBeLessThan(0);
    [Fact] public void Should_write_itself_with_its_unit() => Two.ToString().ShouldEndWith(" kW");
    [Fact] public void Should_offer_a_named_zero() => Sim.SharedKernel.Kilowatts.Zero.Value.ShouldBe(0);
}
