using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Behaviours;
using Sim.Simulation.Parameters;

namespace Sim.Simulation.Domain;

/// <summary>
/// AGGREGATE ROOT of the Simulation context: one run of the simulation process
/// (ADR-0013 - the process IS the business, not a service). The run owns every
/// piece of state that crosses ticks: the clock, each meter's behaviour and the
/// battery's physical charge. Invariants: simulated time only moves forward;
/// every advance produces exactly one reading per meter; storage is commanded
/// at most once per tick and only for a tick already advanced. That ordering is
/// the design: Advance() measures the non-storage meters first, so their sum is
/// the load WITHOUT the battery, and ApplyStorageSetpoint() acts second, after
/// Control has decided. An IoT gateway would replace this class and emit the
/// same PowerReading contract - Energy, Control and Accounting would not notice.
/// </summary>
public sealed class SimulationRun
{
    private readonly Neighbourhood _neighbourhood;
    private readonly SimulationClock _clock;
    private readonly WeatherModel _weather;
    private readonly IReadOnlyDictionary<string, IAssetBehaviour> _behaviours;
    private readonly BatterySimulator? _battery;
    private readonly ulong _seed;

    private TickTelemetry? _lastTelemetry;
    private long _storageCommandedForTick = -1;

    public SimulationRun(Neighbourhood neighbourhood, ulong seed, DateTimeOffset start,
        TimeSpan tickDuration, SimulationProfiles? profiles = null)
    {
        var effective = profiles ?? SimulationProfiles.Default;
        _neighbourhood = neighbourhood;
        _seed = seed;
        _clock = new SimulationClock(start, tickDuration);
        _weather = new WeatherModel(seed, effective.Weather);
        _behaviours = neighbourhood.AllAssets.ToDictionary(a => a.MeterId, a => CreateBehaviourFor(a, effective));
        _battery = neighbourhood.Battery is { } spec ? new BatterySimulator(spec) : null;
    }

    private IReadOnlyList<PowerReading> MeasureEveryMeter(SimulationTick tick)
    {
        var readings = new List<PowerReading>(_neighbourhood.AllAssets.Count);
        foreach (var asset in _neighbourhood.AllAssets)
            readings.Add(new PowerReading(asset.MeterId, tick.Instant, _behaviours[asset.MeterId].PowerAt(asset, tick)));
        return readings;
    }

    private IReadOnlyCollection<string> CollectOccupiedChargePoints() =>
        _behaviours.Where(b => b.Value is PublicChargerBehaviour { Busy: true })
                   .Select(b => b.Key)
                   .ToHashSet();

    private void RefuseUnlessStorageCanBeCommanded()
    {
        if (_battery is null)
            throw new SimulationInvariantViolation("This run has no battery to command.");
        if (_lastTelemetry is null)
            throw new SimulationInvariantViolation("Storage can only be commanded for a tick that has been advanced.");
        if (_storageCommandedForTick == _lastTelemetry.TickIndex)
            throw new SimulationInvariantViolation($"Storage was already commanded for tick {_lastTelemetry.TickIndex}.");
    }

    private static IAssetBehaviour CreateBehaviourFor(Asset asset, SimulationProfiles profiles)
    {
        var stream = DeterministicNoise.StreamOf(asset.MeterId);
        return asset.Type switch
        {
            AssetType.BaseLoad => new BaseLoadBehaviour(stream, profiles.BaseLoadShape),
            AssetType.Pv => new PvBehaviour(),
            AssetType.HeatPump => new HeatPumpBehaviour(stream, profiles.HeatPumpBalancePointC),
            AssetType.HomeEvCharger => new HomeEvChargerBehaviour(stream, profiles.HomeCharger),
            AssetType.PublicEvCharger => new PublicChargerBehaviour(stream, profiles.PublicCharger),
            _ => throw new SimulationInvariantViolation($"No behaviour exists for asset type {asset.Type}."),
        };
    }

    public StorageState? Storage => _battery is null
        ? null
        : new StorageState(_battery.StateOfChargeKwh, _battery.CapacityKwh, _battery.StateOfChargePercent);

    public TickTelemetry Advance()
    {
        var (index, instant) = _clock.NextTick();
        var weather = _weather.At(instant);
        var readings = MeasureEveryMeter(new SimulationTick(index, instant, _clock.TickDuration, weather, _seed));
        return _lastTelemetry = new TickTelemetry(
            index, instant, _clock.TickDuration, weather, readings, CollectOccupiedChargePoints());
    }

    public PowerReading ApplyStorageSetpoint(StorageSetpoint setpoint)
    {
        RefuseUnlessStorageCanBeCommanded();
        _storageCommandedForTick = _lastTelemetry!.TickIndex;
        return _battery!.Apply(setpoint, _lastTelemetry.Instant, _lastTelemetry.Duration);
    }
}

public sealed record StorageState(double StateOfChargeKwh, double CapacityKwh, double StateOfChargePercent);

/// <summary>Raised when a rule of the Simulation context would be violated. One type for the whole context: the message names the field and the rule.</summary>
public sealed class SimulationInvariantViolation(string message) : InvalidOperationException(message);
