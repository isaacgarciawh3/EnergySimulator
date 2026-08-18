using Sim.Application.Engine;

namespace Sim.Api;

/// <summary>
/// The clock driver. This is the "worker" we did not build as a separate
/// deployable (ADR-004): an in-process BackgroundService advancing the
/// simulation at the configured rate. In a scaled system this is a separate
/// container consuming a schedule; the engine API it calls would not change.
/// </summary>
public sealed class SimulationWorker(SimulationEngine engine, ILogger<SimulationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        engine.Start();
        logger.LogInformation("Simulation started at {Instant} with seed {Seed}",
            engine.Snapshot().Instant, engine.Configuration.Seed);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromSeconds(1.0 / Math.Max(0.5, engine.Configuration.TicksPerSecond));
            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            if (engine.Running) engine.Tick();
        }
    }
}
