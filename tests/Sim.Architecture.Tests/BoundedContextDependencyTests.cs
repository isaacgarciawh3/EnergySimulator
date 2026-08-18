using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace Sim.Architecture.Tests;

/// <summary>
/// The claim this solution makes is that the four contexts are genuinely
/// independent - that the simulation could be replaced by a telemetry feed
/// without Energy or Accounting noticing. A claim like that decays into a
/// diagram in a README unless something fails the build when it stops being
/// true, so it is asserted here.
///
/// Each rule is checked twice, deliberately. NetArchTest walks type references,
/// which catches a dependency the moment a type is touched. The assembly manifest
/// check is coarser but immune to the fluent API and to how the compiler trims
/// unused references, and it is the one that would still work if NetArchTest
/// stopped understanding a future target framework.
/// </summary>
public sealed class BoundedContextDependencyTests
{
    private static readonly Assembly Energy = typeof(Sim.Energy.Domain.Asset).Assembly;
    private static readonly Assembly Simulation = typeof(Sim.Simulation.NeighbourhoodSimulator).Assembly;
    private static readonly Assembly Accounting = typeof(Sim.Accounting.Domain.EnergyLedger).Assembly;
    private static readonly Assembly Control = typeof(Sim.Control.Domain.GridState).Assembly;

    private static void ShouldNotDependOn(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespaceStartingWith("Sim.")
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"{assembly.GetName().Name} must not depend on [{string.Join(", ", forbidden)}], " +
            $"but these types do: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    private static void ManifestShouldNotReference(Assembly assembly, params string[] forbidden)
    {
        var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        foreach (var name in forbidden)
            referenced.ShouldNotContain(name,
                $"{assembly.GetName().Name} references {name} in its assembly manifest.");
    }

    // 22
    [Fact]
    public void Energy_does_not_depend_on_simulation_accounting_or_control()
    {
        ShouldNotDependOn(Energy, "Sim.Simulation", "Sim.Accounting", "Sim.Control");
        ManifestShouldNotReference(Energy, "Sim.Simulation", "Sim.Accounting", "Sim.Control");
    }

    // 23
    [Fact]
    public void Accounting_does_not_depend_on_energy_simulation_or_control()
    {
        ShouldNotDependOn(Accounting, "Sim.Energy", "Sim.Simulation", "Sim.Control");
        ManifestShouldNotReference(Accounting, "Sim.Energy", "Sim.Simulation", "Sim.Control");
    }

    // 24
    [Fact]
    public void Control_does_not_depend_on_energy_simulation_or_accounting()
    {
        ShouldNotDependOn(Control, "Sim.Energy", "Sim.Simulation", "Sim.Accounting");
        ManifestShouldNotReference(Control, "Sim.Energy", "Sim.Simulation", "Sim.Accounting");
    }

    // 25
    [Theory]
    [InlineData("Sim.Energy")]
    [InlineData("Sim.Simulation")]
    [InlineData("Sim.Accounting")]
    [InlineData("Sim.Control")]
    public void No_bounded_context_depends_on_the_web_host_or_the_database(string contextName)
    {
        var assembly = ContextNamed(contextName);

        ShouldNotDependOn(assembly, "Microsoft.AspNetCore", "Microsoft.Data.Sqlite", "Sim.Infrastructure", "Sim.Api");
        ManifestShouldNotReference(assembly, "Microsoft.Data.Sqlite", "Sim.Infrastructure", "Sim.Api", "Sim.Application");

        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ShouldNotContain(n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    // 25
    [Fact]
    public void The_only_thing_the_contexts_share_is_the_shared_kernel()
    {
        // Sim.Simulation is allowed to know Energy (it reads the model to produce
        // readings) and Control (it applies setpoints). Nothing else crosses.
        var allowed = new HashSet<string>
        {
            "Sim.SharedKernel", "Sim.Energy", "Sim.Control",
            "System.Runtime", "System.Linq", "System.Collections", "netstandard", "System.Private.CoreLib",
        };

        foreach (var assembly in new[] { Energy, Accounting, Control, Simulation })
        {
            var simReferences = assembly.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(n => n.StartsWith("Sim.", StringComparison.Ordinal));

            foreach (var reference in simReferences)
                allowed.ShouldContain(reference, $"{assembly.GetName().Name} references {reference}.");
        }

        // Energy is nameplate data in plain doubles, so the compiler trims even the
        // shared kernel out of its manifest: the context reaches for nothing at all.
        SimReferencesOf(Energy).ShouldBeEmpty();
        SimReferencesOf(Accounting).ShouldBe(["Sim.SharedKernel"]);
        SimReferencesOf(Control).ShouldBe(["Sim.SharedKernel"]);
    }

    private static IReadOnlyList<string> SimReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.StartsWith("Sim.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

    private static Assembly ContextNamed(string name) => name switch
    {
        "Sim.Energy" => Energy,
        "Sim.Simulation" => Simulation,
        "Sim.Accounting" => Accounting,
        "Sim.Control" => Control,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown bounded context."),
    };
}
