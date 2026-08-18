using System.Security.Cryptography;
using System.Text;
using Shouldly;
using Sim.Application.Configuration;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests;

/// <summary>
/// GOLDEN MASTER for the TASK-015 structural refactor. It locks the exact
/// telemetry the simulation produces - every meter, every tick, full double
/// precision - so the refactor is provably behaviour-preserving: if this hash
/// changes, the refactor changed behaviour and must not merge.
///
/// The construction lines below are the ONLY lines allowed to change during
/// the refactor. The fingerprint is not.
/// </summary>
public class SimulationCharacterizationTest
{
    private const string LockedFingerprint = "198AD6956D53CC5AE8194D4570AB118587FE0892462A94C3BB121D47B0185FDF";

    [Fact]
    public void Two_hundred_ticks_of_the_default_world_produce_the_locked_sequence() =>
        SequenceFingerprint().ShouldBe(LockedFingerprint);

    private static string SequenceFingerprint()
    {
        var configuration = SimulationConfiguration.Default;
        var neighbourhood = NeighbourhoodBuilder.Build(configuration);
        var run = new SimulationRun(neighbourhood, unchecked((ulong)configuration.Seed),
            configuration.StartInstant, TimeSpan.FromMinutes(15));

        var text = new StringBuilder();
        for (var i = 0; i < 200; i++)
        {
            var telemetry = run.Advance();
            text.Append(telemetry.Instant.ToString("O")).Append('#').Append(telemetry.Weather.TemperatureC.ToString("R"));
            foreach (var reading in telemetry.Readings)
                text.Append('|').Append(reading.MeterId).Append('=').Append(reading.Power.Value.ToString("R"));
            text.Append('\n');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
