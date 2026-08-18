using FsCheck.Xunit;
using Shouldly;
using Sim.Accounting.Domain;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests;

/// <summary>
/// The assignment asks for energy that adds up. These are the tests that prove
/// it, and they are the reason the ledger is its own bounded context: none of
/// them mentions a house, a heat pump or the weather. A meter drew power or it
/// delivered power, and the sign says which.
/// </summary>
public sealed class EnergyAccountingTests
{
    // 1
    [Property(MaxTest = 500)]
    public void Ledger_conservation_holds_for_any_set_of_readings(int[]? centiKilowatts)
    {
        var settlement = new EnergyLedger().Post(Instant, Quarter, ReadingsFrom(centiKilowatts));

        // Everything that entered the neighbourhood must equal everything that left it:
        // locally generated power plus imported power == consumed power plus exported power.
        var supplied = settlement.Generation.Value + settlement.Import.Value;
        var absorbed = settlement.Consumption.Value + settlement.Export.Value;

        supplied.ShouldBe(absorbed, Close(supplied, absorbed));
    }

    // 2
    [Property(MaxTest = 500)]
    public void Per_meter_accounts_sum_to_the_ledger_totals(int[]? centiKilowatts)
    {
        var readings = ReadingsFrom(centiKilowatts);
        var ledger = new EnergyLedger();

        // Two intervals, so accumulation across posts is exercised and not just a single settlement.
        ledger.Post(Instant, Quarter, readings);
        ledger.Post(Instant + Quarter, Quarter, readings);

        var perMeter = ledger.Accounts.Sum(a => a.Consumed.Value - a.Generated.Value);
        var total = ledger.TotalConsumed.Value - ledger.TotalGenerated.Value;

        perMeter.ShouldBe(total, Close(perMeter, total));
    }

    // 3
    [Property(MaxTest = 500)]
    public void Import_and_export_are_mutually_exclusive(int[]? centiKilowatts)
    {
        var settlement = new EnergyLedger().Post(Instant, Quarter, ReadingsFrom(centiKilowatts));

        settlement.Import.Value.ShouldBeGreaterThanOrEqualTo(0);
        settlement.Export.Value.ShouldBeGreaterThanOrEqualTo(0);
        Math.Min(settlement.Import.Value, settlement.Export.Value).ShouldBe(0, AbsoluteTolerance);
    }

    // 4
    [Fact]
    public void A_purely_generating_neighbourhood_exports_and_never_imports()
    {
        var readings = new[]
        {
            new PowerReading("pv-a", Instant, new Kilowatts(-6.0)),
            new PowerReading("pv-b", Instant, new Kilowatts(-4.0)),
        };

        var ledger = new EnergyLedger();
        var settlement = ledger.Post(Instant, Quarter, readings);

        settlement.Generation.Value.ShouldBe(10.0, AbsoluteTolerance);
        settlement.Consumption.Value.ShouldBe(0, AbsoluteTolerance);
        settlement.Export.Value.ShouldBe(10.0, AbsoluteTolerance);
        settlement.Import.Value.ShouldBe(0, AbsoluteTolerance);
        settlement.NetPower.Value.ShouldBe(-10.0, AbsoluteTolerance);
        ledger.TotalExported.Value.ShouldBe(2.5, AbsoluteTolerance);   // 10 kW x 0.25 h
        ledger.TotalImported.Value.ShouldBe(0, AbsoluteTolerance);
    }

    // 4
    [Fact]
    public void A_purely_consuming_neighbourhood_imports_and_never_exports()
    {
        var readings = new[]
        {
            new PowerReading("load-a", Instant, new Kilowatts(6.0)),
            new PowerReading("load-b", Instant, new Kilowatts(4.0)),
        };

        var ledger = new EnergyLedger();
        var settlement = ledger.Post(Instant, Quarter, readings);

        settlement.Consumption.Value.ShouldBe(10.0, AbsoluteTolerance);
        settlement.Generation.Value.ShouldBe(0, AbsoluteTolerance);
        settlement.Import.Value.ShouldBe(10.0, AbsoluteTolerance);
        settlement.Export.Value.ShouldBe(0, AbsoluteTolerance);
        settlement.NetPower.Value.ShouldBe(10.0, AbsoluteTolerance);
        ledger.TotalImported.Value.ShouldBe(2.5, AbsoluteTolerance);
        ledger.TotalExported.Value.ShouldBe(0, AbsoluteTolerance);
    }

    // 5
    [Fact]
    public void Energy_accumulates_as_power_times_duration()
    {
        var ledger = new EnergyLedger();

        ledger.Post(Instant, Quarter, [new PowerReading("kettle", Instant, new Kilowatts(4.0))]);

        // 4 x 0.25 is exact in binary floating point, so this one is asserted exactly.
        ledger.TotalConsumed.Value.ShouldBe(1.0);
        ledger.Accounts.Single().Consumed.Value.ShouldBe(1.0);
        ledger.Accounts.Single().Net.Value.ShouldBe(1.0);
    }

    // 5
    [Fact]
    public void Generated_energy_accumulates_with_the_same_arithmetic_and_the_opposite_sign()
    {
        var ledger = new EnergyLedger();

        ledger.Post(Instant, Quarter, [new PowerReading("roof", Instant, new Kilowatts(-4.0))]);

        ledger.TotalGenerated.Value.ShouldBe(1.0);
        ledger.TotalConsumed.Value.ShouldBe(0.0);
        ledger.Accounts.Single().Generated.Value.ShouldBe(1.0);
        ledger.Accounts.Single().Net.Value.ShouldBe(-1.0);
    }

    // 5
    [Fact]
    public void Energy_scales_with_the_interval_length_not_the_number_of_posts()
    {
        var quarterly = new EnergyLedger();
        for (var i = 0; i < 4; i++)
            quarterly.Post(Instant + i * Quarter, Quarter, [new PowerReading("kettle", Instant, new Kilowatts(4.0))]);

        var hourly = new EnergyLedger();
        hourly.Post(Instant, TimeSpan.FromHours(1), [new PowerReading("kettle", Instant, new Kilowatts(4.0))]);

        quarterly.TotalConsumed.Value.ShouldBe(hourly.TotalConsumed.Value, Close(4.0, 4.0));
        hourly.TotalConsumed.Value.ShouldBe(4.0, AbsoluteTolerance);
    }
}
