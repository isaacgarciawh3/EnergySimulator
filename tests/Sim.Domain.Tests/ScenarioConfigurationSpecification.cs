using Shouldly;
using Sim.Application.Configuration;
using Sim.Application.Ports;

namespace Sim.Domain.Tests;

/// <summary>In-memory stand-in for the SQLite repository. The port is small enough to implement by hand.</summary>
internal sealed class InMemoryConfigurationRepository : ISimulationConfigurationRepository
{
    private SimulationConfiguration? _stored;

    public SimulationConfiguration? Find() => _stored;
    public void Save(SimulationConfiguration configuration) => _stored = configuration;
    public bool Exists() => _stored is not null;
    public void Clear() => _stored = null;
}

/// <summary>
/// The scenario comes from the configuration file, and the precedence between
/// file, stored row and hardcoded fallback is a decision worth defending
/// (ADR-0012, A-012).
/// </summary>
public class TheScenarioConfigurationSpecification
{
    private static ScenarioSettings AFileScenario => new()
    {
        Seed = 4242,
        StartInstant = "2026-06-21T00:00:00+00:00",
        TickMinutes = 30,
        PvShare = 0.5,
    };

    [Fact]
    public void Given_a_scenario_in_the_file_When_converted_Then_every_field_reaches_the_configuration()
    {
        var configuration = AFileScenario.ToConfiguration();

        configuration.Seed.ShouldBe(4242);
        configuration.TickMinutes.ShouldBe(30);
        configuration.PvShare.ShouldBe(0.5);
        configuration.StartInstant.ShouldBe(new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Given_nothing_stored_yet_When_the_repository_is_asked_Then_it_answers_null_rather_than_inventing_defaults()
    {
        var repository = new InMemoryConfigurationRepository();

        repository.Find().ShouldBeNull();
        repository.Exists().ShouldBeFalse();
    }

    [Fact]
    public void Given_a_stored_configuration_When_it_is_cleared_Then_the_next_read_falls_back_to_the_file()
    {
        var repository = new InMemoryConfigurationRepository();
        repository.Save(AFileScenario.ToConfiguration() with { Seed = 999 });
        repository.Find()!.Seed.ShouldBe(999);

        repository.Clear();

        repository.Find().ShouldBeNull();
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("")]
    public void Given_an_unparseable_start_instant_When_the_scenario_is_read_Then_the_boot_fails_loudly(string instant)
    {
        var scenario = new ScenarioSettings { StartInstant = instant };

        Should.Throw<InvalidOperationException>(() => scenario.ToConfiguration())
              .Message.ShouldContain("StartInstant");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Given_a_tick_size_outside_the_supported_range_When_the_scenario_is_read_Then_the_boot_fails(int minutes)
    {
        var scenario = new ScenarioSettings { TickMinutes = minutes };

        Should.Throw<InvalidOperationException>(() => scenario.ToConfiguration())
              .Message.ShouldContain("TickMinutes");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Given_an_asset_share_that_is_not_a_fraction_When_the_scenario_is_read_Then_the_boot_fails(double share)
    {
        var scenario = new ScenarioSettings { PvShare = share };

        Should.Throw<InvalidOperationException>(() => scenario.ToConfiguration())
              .Message.ShouldContain("PvShare");
    }

    [Fact]
    public void Given_a_zero_round_trip_efficiency_When_the_scenario_is_read_Then_the_boot_fails()
    {
        var scenario = new ScenarioSettings { BatteryRoundTripEfficiency = 0 };

        Should.Throw<InvalidOperationException>(() => scenario.ToConfiguration())
              .Message.ShouldContain("BatteryRoundTripEfficiency");
    }

    [Fact]
    public void Given_a_scenario_file_with_no_values_When_read_Then_it_still_produces_a_usable_world()
    {
        var configuration = new ScenarioSettings().ToConfiguration().Validated();

        NeighbourhoodBuilder.Build(configuration).Houses.Count.ShouldBe(30);
    }

    [Fact]
    public void Given_a_hostile_scenario_When_validated_Then_the_house_and_charger_counts_are_still_untouchable()
    {
        // Nothing a file or an API payload can say may move the invariants.
        var hostile = (new ScenarioSettings().ToConfiguration() with
        {
            PvShare = 99, HeatPumpShare = -5, HomeEvShare = 42,
        }).Validated();

        var neighbourhood = NeighbourhoodBuilder.Build(hostile);

        neighbourhood.Houses.Count.ShouldBe(30);
        neighbourhood.PublicChargePoints.Count.ShouldBe(6);
    }
}
