using System.Text.RegularExpressions;
using Shouldly;

namespace Sim.Architecture.Tests;

/// <summary>
/// Determinism is the property the whole engine rests on: same configuration and
/// same seed, same run (ADR-0006). It is broken by reaching for ambient state -
/// the wall clock, unseeded randomness, a fresh Guid.
///
/// NetArchTest cannot catch these. It walks TYPE references, and
/// <c>DateTime.Now</c> is a property call on a type the domain legitimately uses
/// everywhere. So this rule is enforced by reading the source instead. It is a
/// coarser tool, and it is the honest one for this particular rule.
/// </summary>
public sealed class DeterminismRuleTests
{
    private static readonly string[] ProductionProjects =
    [
        "Sim.SharedKernel", "Sim.Energy", "Sim.Simulation", "Sim.Control", "Sim.Accounting", "Sim.Application",
    ];

    /// <summary>Walks up from the test binary until it finds the solution file.</summary>
    private static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("Sim.slnx").Any())
            directory = directory.Parent;

        directory.ShouldNotBeNull("Could not locate the repository root from the test binary.");
        return directory;
    }

    private static IEnumerable<(string File, string Text)> ProductionSources()
    {
        var src = Path.Combine(RepositoryRoot().FullName, "src");

        foreach (var project in ProductionProjects)
        {
            var projectDirectory = Path.Combine(src, project);
            if (!Directory.Exists(projectDirectory)) continue;

            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;

                yield return (Path.GetRelativePath(src, file), File.ReadAllText(file));
            }
        }
    }

    private static void NoProductionSourceMayContain(string pattern, string why)
    {
        var offenders = ProductionSources()
            .Where(s => Regex.IsMatch(s.Text, pattern))
            .Select(s => s.File)
            .ToList();

        offenders.ShouldBeEmpty($"{why} Offending files: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Sanity_check_the_scanner_actually_finds_the_production_sources()
    {
        // A rule that silently scans zero files passes forever. This is what
        // stops these tests from being decorative.
        var sources = ProductionSources().ToList();

        sources.Count.ShouldBeGreaterThan(20, "The determinism scan found almost no source files.");
        sources.ShouldContain(s => s.File.Contains("Neighbourhood"), "Expected the Energy context in the scan.");
        sources.ShouldContain(s => s.File.Contains("WeatherModel"), "Expected the weather model in the scan.");
    }

    [Fact]
    public void No_production_code_reads_the_wall_clock()
    {
        // Simulated time comes from SimulationRun. Any other clock makes two runs
        // of the same seed differ, and makes a replay impossible.
        NoProductionSourceMayContain(
            @"DateTime\s*\.\s*(Now|UtcNow|Today)|DateTimeOffset\s*\.\s*(Now|UtcNow)",
            "Simulated time must come from the simulation clock, never the wall clock.");
    }

    [Fact]
    public void No_production_code_uses_unseeded_randomness()
    {
        // Randomness enters through DeterministicNoise, keyed by an explicit seed.
        NoProductionSourceMayContain(
            @"new\s+Random\s*\(\s*\)|Random\s*\.\s*Shared",
            "Randomness must come from DeterministicNoise with an explicit seed.");
    }

    [Fact]
    public void No_production_code_generates_identifiers_out_of_thin_air()
    {
        // Meter ids are derived from the neighbourhood structure, so the same
        // seed always names the same meters.
        NoProductionSourceMayContain(
            @"Guid\s*\.\s*NewGuid",
            "Identifiers must be derived from the model, not generated per run.");
    }

    [Fact]
    public void The_domain_contexts_do_not_perform_input_or_output()
    {
        // Reading a file or opening a socket inside a context would make it
        // untestable and would smuggle infrastructure past the port boundary.
        var contexts = new[] { "Sim.Energy", "Sim.Simulation", "Sim.Control", "Sim.Accounting" };
        var src = Path.Combine(RepositoryRoot().FullName, "src");

        var offenders = contexts
            .Select(c => Path.Combine(src, c))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f),
                @"System\.IO|File\s*\.\s*(ReadAll|WriteAll|Open)|HttpClient|SqlConnection"))
            .Select(f => Path.GetRelativePath(src, f))
            .ToList();

        offenders.ShouldBeEmpty($"A bounded context is performing I/O: {string.Join(", ", offenders)}");
    }
}
