using Shouldly;
using Sim.Application.Configuration;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests.SimulationRunScenario;

/// <summary>
/// RNF (reproducibility) asserted BIT FOR BIT - no tolerance anywhere here. Two
/// runs of the same seed must produce the same doubles; anything weaker lets a
/// hidden Random or a dictionary ordering slip in. A whole day is expensive, so
/// the fixture walks both runs once.
/// </summary>
public sealed class Two_identically_seeded_runs_walked_for_a_day
{
    public bool ReadingSequencesIdentical { get; } = true;
    public bool WeatherSequencesIdentical { get; } = true;
    public int FewestReadingsInATick { get; } = int.MaxValue;
    public string FirstDivergence { get; } = "none";

    public Two_identically_seeded_runs_walked_for_a_day()
    {
        var configuration = SimulationConfiguration.Default;
        var left = RunFrom(configuration);
        var right = RunFrom(configuration);

        for (var tick = 0; tick < 96; tick++)
        {
            var l = left.Advance();
            var r = right.Advance();

            if (!l.Weather.Equals(r.Weather)) WeatherSequencesIdentical = false;
            FewestReadingsInATick = Math.Min(FewestReadingsInATick, l.Readings.Count);

            if (l.Readings.Count != r.Readings.Count) { ReadingSequencesIdentical = false; continue; }
            for (var i = 0; i < l.Readings.Count; i++)
            {
                if (l.Readings[i].MeterId == r.Readings[i].MeterId
                    && l.Readings[i].Power.Value.Equals(r.Readings[i].Power.Value)) continue;
                if (ReadingSequencesIdentical)
                    FirstDivergence = $"tick {tick}, meter {l.Readings[i].MeterId}";
                ReadingSequencesIdentical = false;
            }
        }
    }

    private static SimulationRun RunFrom(SimulationConfiguration configuration) =>
        new(NeighbourhoodBuilder.Build(configuration), unchecked((ulong)configuration.Seed),
            configuration.StartInstant, configuration.TickDuration);
}

public class When_two_runs_share_the_same_seed(Two_identically_seeded_runs_walked_for_a_day day)
    : IClassFixture<Two_identically_seeded_runs_walked_for_a_day>
{
    [Fact]
    public void Should_produce_byte_identical_reading_sequences() =>
        day.ReadingSequencesIdentical.ShouldBeTrue($"first divergence: {day.FirstDivergence}");

    [Fact]
    public void Should_produce_identical_weather_all_day() =>
        day.WeatherSequencesIdentical.ShouldBeTrue();

    [Fact]
    public void Should_always_answer_for_more_meters_than_there_are_houses() =>
        day.FewestReadingsInATick.ShouldBeGreaterThan(30);
}
