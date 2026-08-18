using Shouldly;
using Sim.Energy.Domain;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.SimulationRunScenario.RunScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>
/// RF-05/RF-06 (energy per meter, aggregate over time) and RF-14/RF-15 (the UI
/// shows clock and weather): one advance answers for every meter and carries
/// everything its caller will ever need.
/// </summary>
public class When_the_run_advances_one_tick
{
    private readonly Neighbourhood _world;
    private readonly TickTelemetry _telemetry;

    public When_the_run_advances_one_tick()
    {
        _world = World();
        _telemetry = RunOf(_world).Advance();
    }

    [Fact]
    public void Should_produce_exactly_one_reading_per_meter() =>
        _telemetry.Readings.Select(r => r.MeterId).Order()
            .ShouldBe(_world.AllAssets.Select(a => a.MeterId).Order());

    [Fact] public void Should_start_counting_ticks_at_zero() => _telemetry.TickIndex.ShouldBe(0);
    [Fact] public void Should_stand_at_the_start_instant() => _telemetry.Instant.ShouldBe(Instant);
    [Fact] public void Should_keep_the_configured_interval_length() => _telemetry.Duration.ShouldBe(Quarter);
    [Fact] public void Should_carry_the_weather_inside_the_telemetry() => _telemetry.Weather.CloudCover.ShouldBeInRange(0.0, 1.0);
    [Fact] public void Should_report_charge_point_occupancy_as_data() => _telemetry.OccupiedChargePoints.ShouldNotBeNull();
}
