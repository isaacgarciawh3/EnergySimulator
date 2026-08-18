using Shouldly;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.KilowattHoursScenario;

/// <summary>ADR-0002: energy converts back to power only through an explicit duration - mixing the two must not compile, and never guesses.</summary>
public class When_energies_are_combined
{
    private static readonly KilowattHours Two = new(2);
    private static readonly KilowattHours Three = new(3);

    [Fact] public void Should_add_as_plain_numbers() => (Two + Three).Value.ShouldBe(5, AbsoluteTolerance);
    [Fact] public void Should_subtract_keeping_the_sign() => (Two - Three).Value.ShouldBe(-1, AbsoluteTolerance);
    [Fact] public void Should_order_smaller_before_larger() => Two.CompareTo(Three).ShouldBeLessThan(0);
    [Fact] public void Should_write_itself_with_its_unit() => Two.ToString().ShouldEndWith(" kWh");
    [Fact] public void Should_offer_a_named_zero() => Sim.SharedKernel.KilowattHours.Zero.Value.ShouldBe(0);
}
