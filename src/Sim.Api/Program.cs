using Sim.Api;
using Sim.Api.Endpoints;
using Sim.Application.Engine;
using Sim.Application.Ports;
using Sim.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
