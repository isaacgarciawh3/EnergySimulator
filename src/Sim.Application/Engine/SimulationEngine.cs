using Sim.Accounting.Contracts;
using Sim.Accounting.Domain;
using Sim.Application.Configuration;
using Sim.Application.Ports;
using Sim.Application.ReadModels;
using Sim.Application.Translation;
using Sim.Energy.Domain;
using Sim.Simulation.Domain;

namespace Sim.Application.Engine;

/// <summary>
/// The orchestrating use case. It is the ONLY place where all three bounded
/// contexts meet, and it holds one aggregate root from each:
///
///   SimulationRun (Simulation)  ->  what time is it, what is the weather
///   Neighbourhood (Energy)      ->  given that, what power flows
///   EnergyLedger  (Accounting)  ->  given those readings, what do the books say
///
/// Each step's output is translated before it crosses into the next context.
/// </summary>
public sealed class SimulationEngine(
    ISimulationConfigurationStore configurations,
    IProjectionStore projections,
    ITickBus bus)
{
    private readonly Lock _gate = new();

    private SimulationConfiguration _configuration = SimulationConfiguration.Default;
    private SimulationRun _run = null!;
    private Neighbourhood _neighbourhood = null!;
    private EnergyLedger _ledger = null!;
    private GridSettlement? _lastSettlement;
    private DashboardSnapshot? _snapshot;

    public bool Running { get; private set; }
    public SimulationConfiguration Configuration => _configuration;

    /// <summary>Boot: adopt the persisted configuration (seeded on first container start) and warm up.</summary>
    public void Start()
    {
        Apply(configurations.LoadOrSeedDefault(), persist: false);
        Running = true;
    }

    /// <summary>Configuration page: rebuild the whole world from a new seed and restart.</summary>
    public void Reconfigure(SimulationConfiguration configuration)
    {
        Apply(configuration.Validated(), persist: true);
        Running = true;
    }

    public void Pause() => Running = false;
    public void Resume() => Running = true;

    private void Apply(SimulationConfiguration configuration, bool persist)
    {
        lock (_gate)
        {
            _configuration = configuration;
            if (persist) configurations.Save(configuration);

            var seed = unchecked((ulong)configuration.Seed);
            _run = new SimulationRun(seed, configuration.StartInstant, configuration.TickDuration);
            _neighbourhood = NeighbourhoodFactory.Create(seed,
                new NeighbourhoodBlueprint(configuration.PvShare, configuration.HeatPumpShare, configuration.HomeEvShare));
            _ledger = new EnergyLedger();
            _lastSettlement = null;
            projections.Reset();

            // Warm start: replay 24 simulated hours so the chart is full and moving
            // on the first paint. Cheap because the engine is deterministic and pure.
            var warmupTicks = (int)(TimeSpan.FromHours(24) / configuration.TickDuration);
            for (var i = 0; i < warmupTicks; i++) AdvanceOnce();
            _snapshot = BuildSnapshot();
        }
    }

    /// <summary>One tick through all three contexts. Called by the background worker.</summary>
    public void Tick()
    {
        lock (_gate)
        {
            AdvanceOnce();
            var snapshot = _snapshot = BuildSnapshot();
            var point = new SeriesPoint(snapshot.Instant, snapshot.NetPowerKw, snapshot.ConsumptionKw, snapshot.GenerationKw);
            projections.SaveMeterTotals(snapshot.Meters);
            bus.Publish(new TickCompleted(snapshot, point));
        }
    }

    public DashboardSnapshot Snapshot()
    {
        lock (_gate) return _snapshot ??= BuildSnapshot();
    }

    private void AdvanceOnce()
    {
        // 1. Simulation context decides when we are and what the weather is.
        var environment = _run.Advance();

        // 2. Translate across the boundary, then let the Energy context measure.
        var measurement = ContextTranslator.ToMeasurementContext(environment, _run.Seed);
        var readings = _neighbourhood.Measure(measurement);

        // 3. Translate again, then let the Accounting context settle the books.
        var entries = readings.Select(ContextTranslator.ToEnergyEntry).ToList();
        _lastSettlement = _ledger.Post(environment.Instant, environment.Duration, entries);
        _lastEnvironment = environment;
        projections.AppendTick(new SeriesPoint(environment.Instant,
            _lastSettlement.NetPower.Value, _lastSettlement.Consumption.Value, _lastSettlement.Generation.Value));
    }

    private Simulation.Contracts.TickEnvironment _lastEnvironment = null!;

    private DashboardSnapshot BuildSnapshot()
    {
        var env = _lastEnvironment;
        var settlement = _lastSettlement!;
        var accounts = _ledger.Accounts.ToDictionary(a => a.MeterId);

        var meters = _ledger.Accounts
            .Select(a => new MeterTotalView(a.MeterId, a.OwnerId, a.Category,
                Math.Round(a.Consumed.Value, 3), Math.Round(a.Generated.Value, 3),
                Math.Round(a.Net.Value, 3), Math.Round(a.LastPower.Value, 3)))
            .OrderBy(m => m.MeterId, StringComparer.Ordinal)
            .ToList();

        var houses = _neighbourhood.Houses.Select(h =>
        {
            double power = 0, energy = 0;
            foreach (var asset in h.Assets)
                if (accounts.TryGetValue(asset.MeterId, out var acc)) { power += acc.LastPower.Value; energy += acc.Net.Value; }
            return new HouseView(h.Id, h.Assets.Select(a => a.Type.ToString()).ToList(), Math.Round(power, 3), Math.Round(energy, 2));
        }).ToList();

        var chargers = _neighbourhood.PublicChargers.Select(c =>
        {
            accounts.TryGetValue(c.MeterId, out var acc);
            return new ChargerView(c.OwnerId, c.Busy, Math.Round(acc?.LastPower.Value ?? 0, 3), Math.Round(acc?.Consumed.Value ?? 0, 2));
        }).ToList();

        var window = projections.LoadWindow(env.Instant - TimeSpan.FromHours(24));

        return new DashboardSnapshot(
            env.TickIndex, env.Instant, env.Season, Math.Round(env.TemperatureC, 1),
            Math.Round(env.CloudCover, 3), Math.Round(env.IrradianceFactor, 3),
            Math.Round(settlement.NetPower.Value, 3), Math.Round(settlement.Consumption.Value, 3),
            Math.Round(settlement.Generation.Value, 3), Math.Round(settlement.Import.Value, 3),
            Math.Round(settlement.Export.Value, 3),
            Math.Round(_ledger.TotalConsumed.Value, 2), Math.Round(_ledger.TotalGenerated.Value, 2),
            Math.Round(_ledger.TotalImported.Value, 2), Math.Round(_ledger.TotalExported.Value, 2),
            meters, houses, chargers, window, Running,
            _configuration.TicksPerSecond, _configuration.TickMinutes, _configuration.Seed);
    }
}
