using Sim.Application.Configuration;
using Sim.Application.Engine;

namespace Sim.Api.Endpoints;

/// <summary>
/// Driving adapter. Every handler is a one-liner that delegates to the engine —
/// there is no business rule in this file, by design.
/// </summary>
public static class SimulationEndpoints
{
    public static void MapSimulation(this WebApplication app)
    {
        var api = app.MapGroup("/api/simulation");

        api.MapGet("/", (SimulationEngine engine) => Results.Ok(engine.Snapshot()));
        api.MapGet("/configuration", (SimulationEngine engine) => Results.Ok(engine.Configuration));
        api.MapPut("/configuration", (SimulationConfiguration configuration, SimulationEngine engine) =>
        {
            engine.Reconfigure(configuration);
            return Results.Ok(engine.Configuration);
        });
        api.MapPost("/pause", (SimulationEngine engine) => { engine.Pause(); return Results.Ok(new { running = false }); });
        api.MapPost("/resume", (SimulationEngine engine) => { engine.Resume(); return Results.Ok(new { running = true }); });
    }
}
