using Sim.Api;
using Sim.Api.Endpoints;
using Sim.Application.Configuration;
using Sim.Application.Engine;
using Sim.Application.Ports;
using Sim.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// The whole simulated world is described by appsettings.Simulation.json: the
// Scenario section says WHICH world to build, the rest says how the physics
// behave. The file is optional so the application still starts without it, but
// when present it is the source of truth for a first boot (ADR-0012).
builder.Configuration.AddJsonFile("appsettings.Simulation.json", optional: true, reloadOnChange: false);

var simulationParameters = builder.Configuration.GetSection(SimulationParameters.SectionName)
    .Get<SimulationParameters>() ?? new SimulationParameters();
simulationParameters.Validate();
builder.Services.AddSingleton(simulationParameters);

var scenario = builder.Configuration.GetSection(ScenarioSettings.SectionName)
    .Get<ScenarioSettings>() ?? new ScenarioSettings();
scenario.ToConfiguration();   // validate now: a bad scenario must fail the boot, not run
builder.Services.AddSingleton(scenario);

// ---- Composition root: the only place that knows which adapter implements which port ----
var databasePath = builder.Configuration["Simulation:DatabasePath"] ?? "sim.db";
builder.Services.AddSingleton(new SqliteConnectionFactory(databasePath));
builder.Services.AddSingleton<ISimulationConfigurationRepository, SqliteConfigurationRepository>();
builder.Services.AddSingleton<IProjectionStore, SqliteProjectionStore>();
builder.Services.AddSingleton<SimulationEngine>();
builder.Services.AddHostedService<SimulationWorker>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapSimulation();
app.Run();

public partial class Program;
