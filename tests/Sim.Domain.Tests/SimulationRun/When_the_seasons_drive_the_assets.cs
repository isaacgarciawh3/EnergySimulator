using Shouldly;
using Sim.Application.Configuration;
using Sim.Energy.Domain;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>
/// R-15/R-16, proven through the run itself: a summer day yields more solar
/// energy than a winter day, heat pumps work harder in winter, and the sun
/// down means solar silence. One full day per season per asset kind - the
/// fixture walks the four days once.
/// </summary>
public sealed class Four_seasons_of_asset_totals
{
    public double SummerPvKw { get; }
    public double WinterPvKw { get; }
    public double SummerHeatPumpKw { get; }
    public double WinterHeatPumpKw { get; }
    public bool SolarSilentAtNight { get; }

    public Four_seasons_of_asset_totals()
    {
        SummerPvKw = TotalFor(AssetType.Pv, new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero));
        WinterPvKw = TotalFor(AssetType.Pv, new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.Zero));
        SummerHeatPumpKw = TotalFor(AssetType.HeatPump, new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero));
        WinterHeatPumpKw = TotalFor(AssetType.HeatPump, new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.Zero));

        var world = NeighbourhoodBuilder.Build(SimulationConfiguration.Default with { PvShare = 1.0 });
        var midnight = new SimulationRun(world, 20260818, new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        var pv = world.AllAssets.Where(a => a.Type == AssetType.Pv).Select(a => a.MeterId).ToHashSet();
        SolarSilentAtNight = midnight.Advance().Readings.Where(r => pv.Contains(r.MeterId)).All(r => r.Power.Value == 0);
    }

    private static double TotalFor(AssetType type, DateTimeOffset start)
    {
        var world = NeighbourhoodBuilder.Build(SimulationConfiguration.Default with { PvShare = 1.0, HeatPumpShare = 1.0 });
        var run = new SimulationRun(world, 20260818, start, TimeSpan.FromMinutes(15));
        var ids = world.AllAssets.Where(a => a.Type == type).Select(a => a.MeterId).ToHashSet();

        var total = 0.0;
        for (var i = 0; i < 96; i++)
            total += run.Advance().Readings.Where(r => ids.Contains(r.MeterId)).Sum(r => r.Power.Value);
        return total;
    }
}

public class When_the_seasons_drive_the_assets(Four_seasons_of_asset_totals seasons)
    : IClassFixture<Four_seasons_of_asset_totals>
{
    [Fact]
    public void Should_generate_more_solar_in_summer_than_in_winter() =>
        seasons.SummerPvKw.ShouldBeLessThan(seasons.WinterPvKw);   // generation is negative: more = lower

    [Fact]
    public void Should_work_the_heat_pumps_harder_in_winter() =>
        seasons.WinterHeatPumpKw.ShouldBeGreaterThan(seasons.SummerHeatPumpKw);

    [Fact]
    public void Should_keep_solar_silent_while_the_sun_is_down() =>
        seasons.SolarSilentAtNight.ShouldBeTrue();
}
