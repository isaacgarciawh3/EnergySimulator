using Sim.Accounting.Domain;
using Sim.Application.Configuration;
using Sim.Application.Ports;
using Sim.Application.ReadModels;
using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;

namespace Sim.Application.Engine;

/// <summary>
/// The orchestrating use case, and the only place the four contexts meet:
///
///   Energy      -> what exists (neighbourhood, houses, assets, battery)
///   Simulation  -> what everything is doing right now, as PowerReading
///   Control     -> what the battery SHOULD do about it
///   Accounting  -> what all of that means for the books
///
/// The ordering is the design. Non-storage assets are measured first, which
/// gives the net load the neighbourhood would have had WITHOUT the battery.
/// The controller sees that number and commands the battery. Both figures then
/// exist naturally, which is what the peak-shaving visualisation needs.
/// </summary>
public sealed class SimulationEngine(
    ISimulationConfigurationRepository configurations,
    IProjectionStore projections,
    SimulationParameters? parameters = null,
    ScenarioSettings? scenario = null)
{
    private readonly SimulationParameters _parameters = parameters ?? new SimulationParameters();
    private readonly ScenarioSettings? _scenario = scenario;
    private readonly Lock _gate = new();

    private SimulationConfiguration _configuration = SimulationConfiguration.Default;
    private Neighbourhood _neighbourhood = null!;
    private SimulationRun _run = null!;
    private PeakShavingStrategy _strategy = null!;
    private EnergyLedger _ledger = null!;

    private GridSettlement? _settlement;
    private TickTelemetry? _telemetry;
    private double _netWithoutBatteryKw, _lastBatteryKw;
    private double _peakWith, _peakWithout, _chargedKwh, _dischargedKwh;
    private DashboardSnapshot? _snapshot;

    public bool Running { get; private set; }
    public SimulationConfiguration Configuration => _configuration;

    /// <summary>
    /// Boot. Precedence, decided HERE rather than in a persistence adapter
    /// (ADR-0012): a stored row wins, because its existence means an operator
    /// changed something through the UI and a restart should not overrule them.
    /// Otherwise the configuration file supplies the scenario. The hardcoded
    /// fallback applies only when there is no file at all.
    /// </summary>
    public void Start()
    {
        var stored = configurations.Find();
        var scenario = stored ?? _scenario?.ToConfiguration() ?? SimulationConfiguration.Default;

        // Persist on a first boot so the scenario the run started from is a
        // recorded fact rather than something re-derived on every restart.
        Apply(scenario.Validated(), persist: stored is null);
        Running = true;
    }

    /// <summary>Forgets the stored configuration and restarts from the file scenario.</summary>
    public void ResetToFileScenario()
    {
        configurations.Clear();
        Apply((_scenario?.ToConfiguration() ?? SimulationConfiguration.Default).Validated(), persist: false);
        Running = true;
    }

    /// <summary>Where the currently running configuration came from, so the UI can say so.</summary>
    public string ConfigurationOrigin { get; private set; } = "unknown";

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
            ConfigurationOrigin = configurations.Exists() ? "stored" : "configuration file";

            _neighbourhood = NeighbourhoodBuilder.Build(configuration, _parameters);
            _run = new SimulationRun(_neighbourhood, unchecked((ulong)configuration.Seed),
                configuration.StartInstant, configuration.TickDuration, _parameters.ToProfiles());
            _strategy = new PeakShavingStrategy(
                configuration.PeakShavingThresholdKw > 0 ? configuration.PeakShavingThresholdKw : null,
                configuration.BatteryRoundTripEfficiency);
            _ledger = new EnergyLedger();
            _peakWith = _peakWithout = _chargedKwh = _dischargedKwh = 0;
            projections.Reset();

            // Warm start: replay 24 simulated hours so the chart is full and the
            // battery has a realistic state of charge on the first paint.
            var warmup = (int)(TimeSpan.FromHours(24) / configuration.TickDuration);
            for (var i = 0; i < warmup; i++) AdvanceOnce();
            _snapshot = BuildSnapshot();
        }
    }

    public void Tick()
    {
        lock (_gate)
        {
            AdvanceOnce();
            var snapshot = _snapshot = BuildSnapshot();
            projections.SaveMeterTotals(snapshot.Meters);
        }
    }

    /// <summary>
    /// The snapshot is a cached projection, rebuilt on each tick. Run state is
    /// therefore overlaid at read time rather than taken from the cache: pausing
    /// stops the ticks, so a paused engine would otherwise keep serving the
    /// `running: true` baked into the last snapshot it built, forever.
    /// </summary>
    public DashboardSnapshot Snapshot()
    {
        lock (_gate) return (_snapshot ??= BuildSnapshot()) with { Running = Running };
    }

    private void AdvanceOnce()
    {
        // 1. The run advances: one reading per non-storage meter.
        var telemetry = _telemetry = _run.Advance();

        // 2. The load the neighbourhood would have had with no battery at all.
        _netWithoutBatteryKw = telemetry.Readings.Sum(r => r.Power.Value);

        // 3. Control decides. It sees a number and the battery's limits - nothing else.
        var all = new List<PowerReading>(telemetry.Readings);
        _lastBatteryKw = 0;
        if (_run.Storage is { } storage && _neighbourhood.Battery is { } spec)
        {
            var state = new GridState(new Kilowatts(_netWithoutBatteryKw),
                storage.StateOfChargeKwh, spec.CapacityKwh, spec.MaxPowerKw);
            var reading = _run.ApplyStorageSetpoint(_strategy.Decide(state, telemetry.Duration));
            _lastBatteryKw = reading.Power.Value;
            var energy = Math.Abs(_lastBatteryKw) * telemetry.Duration.TotalHours;
            if (_lastBatteryKw > 0) _chargedKwh += energy; else _dischargedKwh += energy;
            all.Add(reading);
        }

        // 4. Accounting settles everything, battery included - it is just another meter.
        _settlement = _ledger.Post(telemetry.Instant, telemetry.Duration, all);

        _peakWith = Math.Max(_peakWith, _settlement.NetPower.Value);
        _peakWithout = Math.Max(_peakWithout, _netWithoutBatteryKw);

        projections.AppendTick(new SeriesPoint(telemetry.Instant,
            _settlement.NetPower.Value, _settlement.Consumption.Value, _settlement.Generation.Value,
            _netWithoutBatteryKw, _lastBatteryKw, _run.Storage?.StateOfChargePercent ?? 0));
    }

    private DashboardSnapshot BuildSnapshot()
    {
        var telemetry = _telemetry!;
        var settlement = _settlement!;
        var accounts = _ledger.Accounts.ToDictionary(a => a.MeterId);

        var meters = _ledger.Accounts
            .Select(a => new MeterTotalView(a.MeterId, OwnerOf(a.MeterId), CategoryOf(a.MeterId),
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

        var chargers = _neighbourhood.PublicChargePoints.Select(c =>
        {
            accounts.TryGetValue(c.MeterId, out var acc);
            return new ChargerView(c.OwnerId, telemetry.OccupiedChargePoints.Contains(c.MeterId),
                Math.Round(acc?.LastPower.Value ?? 0, 3), Math.Round(acc?.Consumed.Value ?? 0, 2));
        }).ToList();

        BatteryView? batteryView = null;
        if (_run.Storage is { } storage && _neighbourhood.Battery is not null)
            batteryView = new BatteryView(
                Math.Round(_lastBatteryKw, 3), Math.Round(storage.StateOfChargeKwh, 2), storage.CapacityKwh,
                Math.Round(storage.StateOfChargePercent, 1),
                _lastBatteryKw > 0.01 ? "charging" : _lastBatteryKw < -0.01 ? "discharging" : "idle",
                _strategy.Name, Math.Round(_chargedKwh, 2), Math.Round(_dischargedKwh, 2));

        return new DashboardSnapshot(
            telemetry.TickIndex, telemetry.Instant, telemetry.Weather.Season.ToString(), Math.Round(telemetry.Weather.TemperatureC, 1),
            Math.Round(telemetry.Weather.CloudCover, 3), Math.Round(telemetry.Weather.IrradianceFactor, 3),
            Math.Round(settlement.NetPower.Value, 3), Math.Round(settlement.Consumption.Value, 3),
            Math.Round(settlement.Generation.Value, 3), Math.Round(settlement.Import.Value, 3),
            Math.Round(settlement.Export.Value, 3),
            Math.Round(_ledger.TotalConsumed.Value, 2), Math.Round(_ledger.TotalGenerated.Value, 2),
            Math.Round(_ledger.TotalImported.Value, 2), Math.Round(_ledger.TotalExported.Value, 2),
            meters, houses, chargers, projections.LoadWindow(telemetry.Instant - TimeSpan.FromHours(24)),
            Running, _configuration.TicksPerSecond, _configuration.TickMinutes, _configuration.Seed,
            batteryView, Math.Round(_netWithoutBatteryKw, 3), _configuration.PeakShavingThresholdKw,
            Math.Round(_peakWith, 2), Math.Round(_peakWithout, 2));
    }

    private string OwnerOf(string meterId) =>
        _neighbourhood.AllAssets.FirstOrDefault(a => a.MeterId == meterId)?.OwnerId
        ?? (meterId == _neighbourhood.Battery?.MeterId ? "neighbourhood" : meterId);

    /// <summary>
    /// The dashboard wants a per-type breakdown, so the join from meter to asset
    /// type happens HERE, at read time. It deliberately does not happen in the
    /// ledger: Accounting must not carry asset vocabulary.
    /// </summary>
    private string CategoryOf(string meterId) =>
        _neighbourhood.AllAssets.FirstOrDefault(a => a.MeterId == meterId)?.Type.ToString()
        ?? (meterId == _neighbourhood.Battery?.MeterId ? "Battery" : "Unknown");
}
