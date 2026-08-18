using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Behaviours;
using Sim.Simulation.Parameters;

namespace Sim.Simulation.Domain;

/// <summary>
/// AGGREGATE ROOT of the Simulation context: one run of the simulation process.
///
/// The run owns everything whose state crosses ticks - the clock, each meter's
/// behaviour (a charging session in progress), and the battery's physical
/// charge. Its invariants:
///
///   - simulated time only moves forward;
///   - every advance produces exactly one reading per meter;
///   - storage is commanded at most once per tick, and only for a tick that
///     has actually been advanced.
///
/// This class is not a service (ADR-0013). Simulating IS the business here, so
/// the process itself is domain: the two public commands below ARE the process,
/// and the private methods are its steps. An IoT gateway would replace this
/// class and emit the same PowerReading contract - Energy, Control and
/// Accounting would not notice.
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

    /// <summary>
    /// Step 1 of every tick, called by the application engine: advance time,
    /// sample the weather, and measure what every non-storage meter is doing.
    /// The sum of these readings is the load the neighbourhood would have had
    /// WITHOUT the battery - which is exactly why storage is a separate,
    /// second step.
    /// </summary>
    public TickTelemetry Advance()
    {
        var (index, instant) = _clock.NextTick();
        var weather = _weather.At(instant);
        var readings = MeasureEveryMeter(new SimulationTick(index, instant, _clock.TickDuration, weather, _seed));
        return _lastTelemetry = new TickTelemetry(
            index, instant, _clock.TickDuration, weather, readings, OccupiedChargePoints());
    }

    /// <summary>
    /// Step 2, called by the engine after Control has decided: the battery
    /// physically responds to the setpoint over the tick just advanced, and its
    /// meter reports what actually happened - which differs from what was asked
    /// whenever the battery cannot comply.
    /// </summary>
    public PowerReading ApplyStorageSetpoint(StorageSetpoint setpoint)
    {
        if (_battery is null)
            throw new InvalidOperationException("This run has no battery to command.");
        if (_lastTelemetry is null)
            throw new InvalidOperationException("Storage can only be commanded for a tick that has been advanced.");
        if (_storageCommandedForTick == _lastTelemetry.TickIndex)
            throw new InvalidOperationException($"Storage was already commanded for tick {_lastTelemetry.TickIndex}.");

        _storageCommandedForTick = _lastTelemetry.TickIndex;
        return _battery.Apply(setpoint, _lastTelemetry.Instant, _lastTelemetry.Duration);
    }

    /// <summary>What the storage currently holds. Control reads this to decide; null when the run has no battery.</summary>
    public StorageState? Storage => _battery is null
        ? null
        : new StorageState(_battery.StateOfChargeKwh, _battery.CapacityKwh, _battery.StateOfChargePercent);

    private IReadOnlyList<PowerReading> MeasureEveryMeter(SimulationTick tick)
    {
        var readings = new List<PowerReading>(_neighbourhood.AllAssets.Count);
        foreach (var asset in _neighbourhood.AllAssets)
            readings.Add(new PowerReading(asset.MeterId, tick.Instant, _behaviours[asset.MeterId].PowerAt(asset, tick)));
        return readings;
    }

    private IReadOnlyCollection<string> OccupiedChargePoints() =>
        _behaviours.Where(b => b.Value is PublicChargerBehaviour { Busy: true })
                   .Select(b => b.Key)
                   .ToHashSet();

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
            _ => throw new ArgumentOutOfRangeException(nameof(asset), $"No behaviour exists for asset type {asset.Type}."),
        };
    }
}

/// <summary>The storage's physical situation, as a value: how much it holds of how much it could.</summary>
public sealed record StorageState(double StateOfChargeKwh, double CapacityKwh, double StateOfChargePercent);
