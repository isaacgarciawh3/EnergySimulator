using Sim.Energy.Domain;
using Sim.Simulation.Parameters;
using Sim.SharedKernel;
using Sim.Simulation.Domain;

namespace Sim.Simulation.Behaviours;

/// <summary>Always-present household consumption (A-008): baseline shaped by a morning and evening curve with deterministic jitter.</summary>
public sealed class BaseLoadBehaviour(ulong stream, IDailyShape shape) : IAssetBehaviour
{
    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var jitter = 0.9 + 0.2 * DeterministicNoise.Sample(tick.Seed, stream, tick.TickIndex);
        return new Kilowatts(asset.RatedPowerKw * shape.At(tick.Instant.TimeOfDay.TotalHours) * jitter);
    }
}

/// <summary>Rooftop PV: capacity scaled by the interval's irradiance. Negative because it generates.</summary>
public sealed class PvBehaviour : IAssetBehaviour
{
    public Kilowatts PowerAt(Asset asset, SimulationTick tick) =>
        new(-asset.RatedPowerKw * tick.Weather.IrradianceFactor);
}

/// <summary>Balance-point heat pump (A-005): draw rises linearly below the balance point, capped at rated power.</summary>
public sealed class HeatPumpBehaviour(ulong stream, double balancePointC) : IAssetBehaviour
{
    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var deficit = Math.Max(0.0, balancePointC - tick.Weather.TemperatureC);
        var demand = Math.Min(asset.RatedPowerKw, asset.ResponseCoefficient * deficit);
        var jitter = 0.95 + 0.1 * DeterministicNoise.Sample(tick.Seed, stream, tick.TickIndex);
        return new Kilowatts(demand * jitter);
    }
}

/// <summary>
/// Home EV charging (A-004): one seeded plug-in per day inside the profile
/// window, one seeded session size, charging until full or until the morning
/// departure wipes what remains. Power is the interval average, so the final
/// partial interval accounts exactly.
/// </summary>
public sealed class HomeEvChargerBehaviour(ulong stream, HomeChargerProfile profile) : IAssetBehaviour
{
    private const ulong PlugInHourSalt = 7;
    private const ulong SessionSizeSalt = 13;

    private double _remainingKwh;
    private long _lastPlugDay = long.MinValue;

    private void PlugInIfTheCarArrives(SimulationTick tick, long day, double hour, double plugInHour)
    {
        if (_remainingKwh > 0 || day <= _lastPlugDay || hour < plugInHour) return;
        _lastPlugDay = day;
        _remainingKwh = DrawTheSessionKwh(tick, day);
    }

    private bool HasDepartedForTheDay(double hour, double plugInHour) =>
        hour >= profile.DepartureHour && hour < plugInHour;

    private Kilowatts DeliverFromTheSession(Asset asset, TimeSpan duration)
    {
        var deliveredKwh = Math.Min(asset.RatedPowerKw * duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / duration.TotalHours);
    }

    private double DrawThePlugInHour(SimulationTick tick, long day) =>
        profile.PlugInFromHour + (profile.PlugInToHour - profile.PlugInFromHour)
            * DeterministicNoise.Sample(tick.Seed, stream ^ PlugInHourSalt, day);

    private double DrawTheSessionKwh(SimulationTick tick, long day) =>
        profile.SessionMinKwh + (profile.SessionMaxKwh - profile.SessionMinKwh)
            * DeterministicNoise.Sample(tick.Seed, stream ^ SessionSizeSalt, day);

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var hour = tick.Instant.TimeOfDay.TotalHours;
        var day = tick.Instant.UtcTicks / TimeSpan.TicksPerDay;
        var plugInHour = DrawThePlugInHour(tick, day);

        PlugInIfTheCarArrives(tick, day, hour, plugInHour);
        if (_remainingKwh <= 0) return Kilowatts.Zero;
        if (HasDepartedForTheDay(hour, plugInHour))
        {
            _remainingKwh = 0;
            return Kilowatts.Zero;
        }
        return DeliverFromTheSession(asset, tick.Duration);
    }
}

/// <summary>
/// Public charge point (A-004): seeded arrivals at the profile's time-of-day
/// rate, one seeded session size, delivery until the session runs out. A busy
/// point rejects arrivals - there is no queue, a documented simplification.
/// </summary>
public sealed class PublicChargerBehaviour(ulong stream, PublicChargerProfile profile) : IAssetBehaviour
{
    private const ulong ArrivalSalt = 17;
    private const ulong SessionSizeSalt = 31;

    private double _remainingKwh;

    private void StartASessionIfADriverArrives(SimulationTick tick)
    {
        if (_remainingKwh > 0) return;
        var probability = profile.EstimateArrivalsPerHour(tick.Instant.TimeOfDay.TotalHours) * tick.Duration.TotalHours;
        if (DeterministicNoise.Sample(tick.Seed, stream ^ ArrivalSalt, tick.TickIndex) >= probability) return;
        _remainingKwh = DrawTheSessionKwh(tick);
    }

    private double DrawTheSessionKwh(SimulationTick tick) =>
        profile.SessionMinKwh + (profile.SessionMaxKwh - profile.SessionMinKwh)
            * DeterministicNoise.Sample(tick.Seed, stream ^ SessionSizeSalt, tick.TickIndex);

    private Kilowatts DeliverFromTheSession(Asset asset, TimeSpan duration)
    {
        var deliveredKwh = Math.Min(asset.RatedPowerKw * duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / duration.TotalHours);
    }

    public bool Busy => _remainingKwh > 0;

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        StartASessionIfADriverArrives(tick);
        return _remainingKwh <= 0 ? Kilowatts.Zero : DeliverFromTheSession(asset, tick.Duration);
    }
}
