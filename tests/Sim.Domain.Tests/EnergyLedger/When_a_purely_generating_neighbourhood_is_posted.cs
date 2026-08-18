using Shouldly;
using Sim.Accounting.Domain;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.EnergyLedgerScenario;

/// <summary>R-11/A-003: all generation and nothing else - everything exports, nothing imports.</summary>
public class When_a_purely_generating_neighbourhood_is_posted
{
    private readonly EnergyLedger _ledger = new();
    private readonly GridSettlement _settlement;

    public When_a_purely_generating_neighbourhood_is_posted() =>
        _settlement = _ledger.Post(Instant, Quarter,
        [
            new PowerReading("pv-a", Instant, new Kilowatts(-6.0)),
            new PowerReading("pv-b", Instant, new Kilowatts(-4.0)),
        ]);

    [Fact] public void Should_sum_the_generation() => _settlement.Generation.Value.ShouldBe(10.0, AbsoluteTolerance);
    [Fact] public void Should_find_no_consumption() => _settlement.Consumption.Value.ShouldBe(0, AbsoluteTolerance);
    [Fact] public void Should_export_everything() => _settlement.Export.Value.ShouldBe(10.0, AbsoluteTolerance);
    [Fact] public void Should_import_nothing() => _settlement.Import.Value.ShouldBe(0, AbsoluteTolerance);
    [Fact] public void Should_report_a_negative_net() => _settlement.NetPower.Value.ShouldBe(-10.0, AbsoluteTolerance);
    [Fact] public void Should_accumulate_the_exported_energy() => _ledger.TotalExported.Value.ShouldBe(2.5, AbsoluteTolerance);
    [Fact] public void Should_accumulate_no_imported_energy() => _ledger.TotalImported.Value.ShouldBe(0, AbsoluteTolerance);
}
