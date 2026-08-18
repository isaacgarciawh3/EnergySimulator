using Sim.Accounting.Domain;
using Sim.Application.Configuration;
using Sim.Application.Ports;
using Sim.Application.ReadModels;
using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation;

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
public sealed class SimulationEngine(ISimulationConfigurationStore configurations, IProjectionStore projections)
{
    private readonly Lock _gate = new();

    private SimulationConfiguration _configuration = SimulationConfiguration.Default;
    private Neighbourhood _neighbourhood = null!;
    private NeighbourhoodSimulator _simulator = null!;
    private BatterySimulator? _battery;
    private PeakShavingStrategy _strategy = null!;
    private EnergyLedger _ledger = null!;

    private GridSettlement? _settlement;
    private Simulation.Domain.SimulationTick? _tick;
    private double _netWithoutBatteryKw, _lastBatteryKw;
    private double _peakWith, _peakWithout, _chargedKwh, _dischargedKwh;
    private DashboardSnapshot? _snapshot;

    public bool Running { get; private set; }
    public SimulationConfiguration Configuration => _configuration;

    public void Start()
    {
        Apply(configurations.LoadOrSeedDefault(), persist: false);
        Running = true;
    }

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

            _neighbourhood = NeighbourhoodBuilder.Build(configuration);
            _simulator = new NeighbourhoodSimulator(_neighbourhood, unchecked((ulong)configuration.Seed),
                configuration.StartInstant, configuration.TickDuration);
            _battery = _neighbourhood.Battery is { } spec ? new BatterySimulator(spec) : null;
            _strategy = new PeakShavingStrategy(configuration.PeakShavingThresholdKw, configuration.BatteryRoundTripEfficiency);
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

    public DashboardSnapshot Snapshot()
    {
        lock (_gate) return _snapshot ??= BuildSnapshot();
    }

    private void AdvanceOnce()
    {
        // 1. Simulation reports what every non-storage meter is doing.
        var (tick, readings) = _simulator.Advance();
        _tick = tick;

        // 2. The load the neighbourhood would have had with no battery at all.
        _netWithoutBatteryKw = readings.Sum(r => r.Power.Value);

        // 3. Control decides. It sees a number and the battery's limits - nothing else.
        var all = new List<PowerReading>(readings);
        _lastBatteryKw = 0;
        if (_battery is { } battery && _neighbourhood.Battery is { } spec)
        {
            var state = new GridState(new Kilowatts(_netWithoutBatteryKw),
                battery.StateOfChargeKwh, spec.CapacityKwh, spec.MaxPowerKw);
            var reading = battery.Apply(_strategy.Decide(state, tick.Duration), tick.Instant, tick.Duration);
            _lastBatteryKw = reading.Power.Value;
            var energy = Math.Abs(_lastBatteryKw) * tick.Duration.TotalHours;
            if (_lastBatteryKw > 0) _chargedKwh += energy; else _dischargedKwh += energy;
            all.Add(reading);
        }

        // 4. Accounting settles everything, battery included - it is just another meter.
        _settlement = _ledger.Post(tick.Instant, tick.Duration, all);

        _peakWith = Math.Max(_peakWith, _settlement.NetPower.Value);
        _peakWithout = Math.Max(_peakWithout, _netWithoutBatteryKw);

        projections.AppendTick(new SeriesPoint(tick.Instant,
            _settlement.NetPower.Value, _settlement.Consumption.Value, _settlement.Generation.Value,
            _netWithoutBatteryKw, _lastBatteryKw, _battery?.StateOfChargePercent ?? 0));
    }

    private DashboardSnapshot BuildSnapshot()
    {
        var tick = _tick!;
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
            return new ChargerView(c.OwnerId, _simulator.IsBusy(c.MeterId),
                Math.Round(acc?.LastPower.Value ?? 0, 3), Math.Round(acc?.Consumed.Value ?? 0, 2));
        }).ToList();

        BatteryView? batteryView = null;
        if (_battery is { } battery && _neighbourhood.Battery is not null)
            batteryView = new BatteryView(
                Math.Round(_lastBatteryKw, 3), Math.Round(battery.StateOfChargeKwh, 2), battery.CapacityKwh,
                Math.Round(battery.StateOfChargePercent, 1),
                _lastBatteryKw > 0.01 ? "charging" : _lastBatteryKw < -0.01 ? "discharging" : "idle",
                _strategy.Name, Math.Round(_chargedKwh, 2), Math.Round(_dischargedKwh, 2));

        return new DashboardSnapshot(
            tick.TickIndex, tick.Instant, tick.Weather.Season.ToString(), Math.Round(tick.Weather.TemperatureC, 1),
            Math.Round(tick.Weather.CloudCover, 3), Math.Round(tick.Weather.IrradianceFactor, 3),
            Math.Round(settlement.NetPower.Value, 3), Math.Round(settlement.Consumption.Value, 3),
            Math.Round(settlement.Generation.Value, 3), Math.Round(settlement.Import.Value, 3),
            Math.Round(settlement.Export.Value, 3),
            Math.Round(_ledger.TotalConsumed.Value, 2), Math.Round(_ledger.TotalGenerated.Value, 2),
            Math.Round(_ledger.TotalImported.Value, 2), Math.Round(_ledger.TotalExported.Value, 2),
            meters, houses, chargers, projections.LoadWindow(tick.Instant - TimeSpan.FromHours(24)),
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
