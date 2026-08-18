using Shouldly;
using Sim.Accounting.Domain;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.EnergyLedgerScenario;

/// <summary>R-10: energy is power times duration - 4 x 0.25 is exact in binary, so this asserts EXACTLY.</summary>
public class When_a_kettle_draws_four_kilowatts_for_a_quarter_hour
{
    private readonly EnergyLedger _ledger = new();

    public When_a_kettle_draws_four_kilowatts_for_a_quarter_hour() =>
        _ledger.Post(Instant, Quarter, [new PowerReading("kettle", Instant, new Kilowatts(4.0))]);

    [Fact] public void Should_accumulate_exactly_one_kilowatt_hour() => _ledger.TotalConsumed.Value.ShouldBe(1.0);
    [Fact] public void Should_post_it_to_the_kettles_account() => _ledger.Accounts.Single().Consumed.Value.ShouldBe(1.0);
    [Fact] public void Should_net_the_account_positive() => _ledger.Accounts.Single().Net.Value.ShouldBe(1.0);
    [Fact] public void Should_remember_the_last_power_seen() => _ledger.Accounts.Single().LastPower.Value.ShouldBe(4.0);
    [Fact] public void Should_know_which_meter_it_is() => _ledger.Accounts.Single().MeterId.ShouldBe("kettle");
}
