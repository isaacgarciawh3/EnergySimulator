using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using Sim.Application.Ports;

namespace Sim.Architecture.Tests;

/// <summary>
/// Context isolation (see <see cref="BoundedContextDependencyTests"/>) says the
/// four contexts do not know each other. This file asserts the other half of the
/// realization: that dependencies only ever point INWARD.
///
/// Sim.Api -> Sim.Infrastructure -> Sim.Application -> contexts -> Sim.SharedKernel
///
/// Every arrow that would point the other way is a rule here.
/// </summary>
public sealed class LayeredDependencyTests
{
    private static readonly Assembly SharedKernel = typeof(Sim.SharedKernel.Kilowatts).Assembly;
    private static readonly Assembly Application = typeof(Sim.Application.Engine.SimulationEngine).Assembly;
    private static readonly Assembly Infrastructure = typeof(Sim.Infrastructure.Persistence.SqliteConnectionFactory).Assembly;

    private static IReadOnlyList<string> SimReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.StartsWith("Sim.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void The_shared_kernel_is_the_innermost_layer_and_depends_on_nothing_of_ours()
    {
        // If the shared kernel ever reaches outward, the whole dependency rule
        // inverts: every context would transitively see whatever it grabbed.
        SimReferencesOf(SharedKernel).ShouldBeEmpty();
    }

    [Fact]
    public void The_shared_kernel_knows_nothing_about_the_web_host_or_the_database()
    {
        SharedKernel.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ShouldNotContain(n =>
                n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
                n.StartsWith("Microsoft.Data", StringComparison.Ordinal) ||
                n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void The_application_layer_does_not_reach_out_to_infrastructure_or_the_web_host()
    {
        // This is the rule that keeps the ports meaningful. The moment the
        // application can see the SQLite adapter, the interface is decoration.
        var result = Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOnAny("Sim.Infrastructure", "Sim.Api", "Microsoft.Data.Sqlite", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Sim.Application must depend only inward, but these types reach outward: " +
            $"{string.Join(", ", result.FailingTypeNames ?? [])}");

        SimReferencesOf(Application).ShouldNotContain("Sim.Infrastructure");
        SimReferencesOf(Application).ShouldNotContain("Sim.Api");
    }

    [Fact]
    public void Infrastructure_does_not_reach_out_to_the_web_host()
    {
        SimReferencesOf(Infrastructure).ShouldNotContain("Sim.Api");

        Infrastructure.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ShouldNotContain(n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_driven_port_is_an_interface_declared_by_the_application_layer()
    {
        // A port is a promise the application makes about what it needs. If one
        // ever appears as a concrete class, or outside the application layer,
        // the direction of control has quietly flipped.
        var ports = new[] { typeof(ISimulationConfigurationRepository), typeof(IProjectionStore) };

        foreach (var port in ports)
        {
            port.IsInterface.ShouldBeTrue($"{port.Name} must be an interface.");
            port.Assembly.ShouldBe(Application, $"{port.Name} must be declared in Sim.Application.");
            port.Namespace.ShouldBe("Sim.Application.Ports");
        }
    }

    [Fact]
    public void Every_adapter_that_implements_a_port_lives_in_infrastructure()
    {
        // The mirror of the rule above: implementations belong on the outside.
        var ports = new[] { typeof(ISimulationConfigurationRepository), typeof(IProjectionStore) };

        foreach (var port in ports)
        {
            var implementations = Infrastructure.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && port.IsAssignableFrom(t))
                .ToList();

            implementations.ShouldNotBeEmpty($"No adapter implements {port.Name}; the port is dead.");
            implementations.ShouldAllBe(t => t.Namespace!.StartsWith("Sim.Infrastructure", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void No_context_or_the_application_layer_depends_on_entity_framework()
    {
        // Not used anywhere, and asserted so that adding it becomes a decision
        // someone has to argue for rather than something that drifts in.
        foreach (var assembly in new[] { SharedKernel, Application, Infrastructure })
            assembly.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .ShouldNotContain(n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
