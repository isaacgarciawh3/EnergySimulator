using Shouldly;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>R-43/A-010 (nameplate + readable state): Control needs charge, capacity and percentage to decide.</summary>
public class When_the_battery_is_freshly_installed
{
    private readonly StorageState _storage;

    public When_the_battery_is_freshly_installed() =>
        _storage = RunOf(World(ADefaultBattery)).Storage.ShouldNotBeNull();

    [Fact] public void Should_report_the_nameplate_capacity() => _storage.CapacityKwh.ShouldBe(100);

    [Fact]
    public void Should_start_half_full_so_the_first_peak_has_something_to_shave() =>
        _storage.StateOfChargeKwh.ShouldBe(50, Close(50, _storage.StateOfChargeKwh));

    [Fact]
    public void Should_match_the_percentage_to_the_charge() =>
        _storage.StateOfChargePercent.ShouldBe(50, Close(50, _storage.StateOfChargePercent));
}
