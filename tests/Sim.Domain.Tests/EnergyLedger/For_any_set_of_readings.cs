using FsCheck.Xunit;
using Shouldly;
using Sim.Accounting.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.EnergyLedgerScenario;

/// <summary>
/// The context's invariants, held for ANY input. Property-based tests are the
/// recorded exception to the constructor-act rule (ADR-0014): FsCheck generates
/// the scenario, so the act lives in the property body.
/// </summary>
public sealed class For_any_set_of_readings
{
    [Property(MaxTest = 500)]
    public void Energy_is_conserved(int[]? centiKilowatts)
    {
        var settlement = new EnergyLedger().Post(Instant, Quarter, ReadingsFrom(centiKilowatts));

        var supplied = settlement.Generation.Value + settlement.Import.Value;
        var absorbed = settlement.Consumption.Value + settlement.Export.Value;
        supplied.ShouldBe(absorbed, Close(supplied, absorbed));
    }

    [Property(MaxTest = 500)]
    public void Per_meter_accounts_sum_to_the_ledger_totals(int[]? centiKilowatts)
    {
        var readings = ReadingsFrom(centiKilowatts);
        var ledger = new EnergyLedger();
        ledger.Post(Instant, Quarter, readings);
        ledger.Post(Instant + Quarter, Quarter, readings);

        var perMeter = ledger.Accounts.Sum(a => a.Consumed.Value - a.Generated.Value);
        var total = ledger.TotalConsumed.Value - ledger.TotalGenerated.Value;
        perMeter.ShouldBe(total, Close(perMeter, total));
    }

    [Property(MaxTest = 500)]
    public void Import_and_export_are_mutually_exclusive(int[]? centiKilowatts)
    {
        var settlement = new EnergyLedger().Post(Instant, Quarter, ReadingsFrom(centiKilowatts));

        Math.Min(settlement.Import.Value, settlement.Export.Value).ShouldBe(0, AbsoluteTolerance);
    }
}
