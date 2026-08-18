using Sim.Api;
using Sim.Api.Endpoints;
using Sim.Application.Configuration;
using Sim.Application.Engine;
using Sim.Application.Ports;
using Sim.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Physical parameters come from appsettings.Simulation.json. The file is optional:
// absent, the shipped defaults apply and the application still starts.
builder.Configuration.AddJsonFile("appsettings.Simulation.json", optional: true, reloadOnChange: false);
var simulationParameters = builder.Configuration.GetSection(SimulationParameters.SectionName)
    .Get<SimulationParameters>() ?? new SimulationParameters();
simulationParameters.Validate();
builder.Services.AddSingleton(simulationParameters);

// ---- Composition root: the only place that knows which adapter implements which port ----
var databasePath = builder.Configuration["Simulation:DatabasePath"] ?? "sim.db";
builder.Services.AddSingleton(new SqliteConnectionFactory(databasePath));
builder.Services.AddSingleton<ISimulationConfigurationStore, SqliteConfigurationStore>();
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
