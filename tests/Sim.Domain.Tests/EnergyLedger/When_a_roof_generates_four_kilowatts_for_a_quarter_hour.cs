using Shouldly;
using Sim.Accounting.Domain;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.EnergyLedgerScenario;

/// <summary>ADR-0002: the same arithmetic with the opposite sign - generation accumulates on its own side.</summary>
public class When_a_roof_generates_four_kilowatts_for_a_quarter_hour
{
    private readonly EnergyLedger _ledger = new();

    public When_a_roof_generates_four_kilowatts_for_a_quarter_hour() =>
        _ledger.Post(Instant, Quarter, [new PowerReading("roof", Instant, new Kilowatts(-4.0))]);

    [Fact] public void Should_accumulate_exactly_one_generated_kilowatt_hour() => _ledger.TotalGenerated.Value.ShouldBe(1.0);
    [Fact] public void Should_consume_nothing() => _ledger.TotalConsumed.Value.ShouldBe(0.0);
    [Fact] public void Should_post_the_generation_to_the_roofs_account() => _ledger.Accounts.Single().Generated.Value.ShouldBe(1.0);
    [Fact] public void Should_net_the_account_negative() => _ledger.Accounts.Single().Net.Value.ShouldBe(-1.0);
}
