using Shouldly;
using Sim.Application.Configuration;
using Sim.Energy.Domain;
using Sim.Simulation;

namespace Sim.Domain.Tests;

/// <summary>
/// Reproducibility is a stated requirement, so it is asserted BIT FOR BIT: no
/// tolerance is used anywhere in this file. Two runs of the same seed must not
/// merely agree to nine decimals, they must produce the same doubles. Anything
/// weaker would let a hidden Random, a dictionary iteration order or a DateTime.Now
/// slip in unnoticed.
/// </summary>
public sealed class DeterminismTests
{
    private const int TicksInADay = 96;

    private static NeighbourhoodSimulator SimulatorFor(SimulationConfiguration configuration) =>
        new(NeighbourhoodBuilder.Build(configuration),
            unchecked((ulong)configuration.Seed),
            configuration.StartInstant,
            configuration.TickDuration);

    private static string LayoutOf(Neighbourhood neighbourhood) =>
        string.Join('\n', neighbourhood.AllAssets.Select(a =>
            $"{a.MeterId}|{a.OwnerId}|{a.Type}|{a.RatedPowerKw:R}|{a.ResponseCoefficient:R}"));

    // 6
    [Fact]
    public void The_same_seed_produces_byte_identical_reading_sequences()
    {
        var configuration = SimulationConfiguration.Default;
        var left = SimulatorFor(configuration);
        var right = SimulatorFor(configuration);

        for (var tick = 0; tick < TicksInADay; tick++)
        {
            var (leftTick, leftReadings) = left.Advance();
            var (rightTick, rightReadings) = right.Advance();

            leftTick.Instant.ShouldBe(rightTick.Instant);
            leftTick.TickIndex.ShouldBe(rightTick.TickIndex);
            leftReadings.Count.ShouldBe(rightReadings.Count);
            leftReadings.Count.ShouldBeGreaterThan(Neighbourhood.RequiredHouses);

            for (var i = 0; i < leftReadings.Count; i++)
            {
                leftReadings[i].MeterId.ShouldBe(rightReadings[i].MeterId);
                // Exact: the claim is reproducibility, not approximate agreement.
                leftReadings[i].Power.Value.ShouldBe(rightReadings[i].Power.Value);
            }
        }
    }

    // 6
    [Fact]
    public void The_same_seed_produces_identical_weather_over_a_whole_day()
    {
        var configuration = SimulationConfiguration.Default;
        var left = SimulatorFor(configuration);
        var right = SimulatorFor(configuration);

        for (var tick = 0; tick < TicksInADay; tick++)
        {
            var (leftTick, _) = left.Advance();
            var (rightTick, _) = right.Advance();
            leftTick.Weather.ShouldBe(rightTick.Weather);
        }
    }

    // 7
    [Fact]
    public void The_same_configuration_produces_the_same_asset_layout()
    {
        var configuration = SimulationConfiguration.Default;

        var first = NeighbourhoodBuilder.Build(configuration);
        var second = NeighbourhoodBuilder.Build(configuration);

        LayoutOf(first).ShouldBe(LayoutOf(second));
        first.AllAssets.ShouldBe(second.AllAssets);           // Asset is a record: structural equality
        first.Houses.Count.ShouldBe(second.Houses.Count);
        first.Battery.ShouldBe(second.Battery);
    }

    // 8
    [Fact]
    public void A_different_seed_produces_a_different_asset_layout()
    {
        var configuration = SimulationConfiguration.Default;

        var first = NeighbourhoodBuilder.Build(configuration with { Seed = 1 });
        var second = NeighbourhoodBuilder.Build(configuration with { Seed = 2 });

        // Guards against the layout being a constant that ignores the seed entirely.
        LayoutOf(first).ShouldNotBe(LayoutOf(second));
    }

    // 8
    [Fact]
    public void A_different_seed_produces_different_readings()
    {
        var configuration = SimulationConfiguration.Default;

        var first = SimulatorFor(configuration with { Seed = 1 }).Advance().Readings;
        var second = SimulatorFor(configuration with { Seed = 2 }).Advance().Readings;

        var identical = first.Count == second.Count
            && first.Zip(second).All(p => p.First.MeterId == p.Second.MeterId
                                       && p.First.Power.Value.Equals(p.Second.Power.Value));

        identical.ShouldBeFalse();
    }
}
