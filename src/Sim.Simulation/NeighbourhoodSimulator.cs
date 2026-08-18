using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Behaviours;
using Sim.Simulation.Domain;
using Sim.Simulation.Parameters;

namespace Sim.Simulation;

/// <summary>
/// Produces telemetry for a neighbourhood by simulating it.
///
/// This is the replaceable half of the system. It reads the Energy model to
/// learn which meters exist and what is installed behind them, then emits a
/// PowerReading per meter per interval. An IoT gateway reading real hardware
/// would occupy exactly this position and emit exactly the same contract,
/// and neither Energy nor Accounting would change.
/// </summary>
public sealed class NeighbourhoodSimulator
{
    private readonly Neighbourhood _neighbourhood;
    private readonly SimulationRun _run;
    private readonly Dictionary<string, IAssetBehaviour> _behaviours;

    private readonly SimulationProfiles _profiles;

    public NeighbourhoodSimulator(Neighbourhood neighbourhood, ulong seed, DateTimeOffset start,
        TimeSpan tickDuration, SimulationProfiles? profiles = null)
    {
        _neighbourhood = neighbourhood;
        _profiles = profiles ?? SimulationProfiles.Default;
        _run = new SimulationRun(seed, start, tickDuration, _profiles.Weather);
        _behaviours = neighbourhood.AllAssets.ToDictionary(a => a.MeterId, Create);
    }

    public DateTimeOffset CurrentInstant => _run.CurrentInstant;
    public long TickIndex => _run.TickIndex;
    public WeatherConditions? LastWeather { get; private set; }

    public bool IsBusy(string meterId) =>
        _behaviours.TryGetValue(meterId, out var b) && b is PublicChargerBehaviour { Busy: true };

    /// <summary>Advances one interval and reports what every meter saw.</summary>
    public (SimulationTick Tick, IReadOnlyList<PowerReading> Readings) Advance()
    {
        var tick = _run.Advance();
        LastWeather = tick.Weather;

        var readings = new List<PowerReading>(_neighbourhood.AllAssets.Count);
        foreach (var asset in _neighbourhood.AllAssets)
            readings.Add(new PowerReading(asset.MeterId, tick.Instant, _behaviours[asset.MeterId].PowerAt(asset, tick)));

        return (tick, readings);
    }

    private IAssetBehaviour Create(Asset asset)
    {
        var stream = DeterministicNoise.StreamOf(asset.MeterId);
        return asset.Type switch
        {
            AssetType.BaseLoad => new BaseLoadBehaviour(stream, _profiles.BaseLoadShape),
            AssetType.Pv => new PvBehaviour(),
            AssetType.HeatPump => new HeatPumpBehaviour(stream, _profiles.HeatPumpBalancePointC),
            AssetType.HomeEvCharger => new HomeEvChargerBehaviour(stream, _profiles.HomeCharger),
            AssetType.PublicEvCharger => new PublicChargerBehaviour(stream, _profiles.PublicCharger),
            _ => throw new ArgumentOutOfRangeException(nameof(asset), $"No behaviour for {asset.Type}."),
        };
    }
}
