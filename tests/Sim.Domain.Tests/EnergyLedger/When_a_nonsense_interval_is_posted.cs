using Shouldly;
using Sim.Accounting.Domain;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.EnergyLedgerScenario;

/// <summary>
/// TASK-024, the latent bug: an interval that does not run forward would feed
/// negative or zero energy into every accumulator and corrupt the books
/// silently. The ledger refuses it as a business rule of its own context.
/// </summary>
public class When_a_nonsense_interval_is_posted
{
    private static readonly PowerReading[] AnyReading = [new("kettle", Instant, new Kilowatts(4.0))];

    private readonly Exception? _zeroInterval =
        Record.Exception(() => new EnergyLedger().Post(Instant, TimeSpan.Zero, AnyReading));

    private readonly Exception? _backwardsInterval =
        Record.Exception(() => new EnergyLedger().Post(Instant, TimeSpan.FromMinutes(-15), AnyReading));

    [Fact] public void Should_refuse_a_zero_interval() => _zeroInterval.ShouldBeOfType<AccountingInvariantViolation>();
    [Fact] public void Should_refuse_a_backwards_interval() => _backwardsInterval.ShouldBeOfType<AccountingInvariantViolation>();
    [Fact] public void Should_explain_what_the_corruption_would_be() => _zeroInterval!.Message.ShouldContain("corrupt every accumulator");
}
