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

/// <summary>Balance-point heat pump (A-005): draw rises linearly below 15 C, capped at rated power.</summary>
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
/// Home EV charging (A-004): one seeded plug-in per day between 17:30 and 19:00
/// needing 8-12 kWh, charging until full or until the 07:00 departure. Reported
/// power is the interval average so the final partial interval accounts exactly.
/// </summary>
public sealed class HomeEvChargerBehaviour(ulong stream, HomeChargerProfile profile) : IAssetBehaviour
{
    private double _remainingKwh;
    private long _lastPlugDay = long.MinValue;

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var hour = tick.Instant.TimeOfDay.TotalHours;
        var day = tick.Instant.UtcTicks / TimeSpan.TicksPerDay;
        var plugInHour = profile.PlugInFromHour
            + (profile.PlugInToHour - profile.PlugInFromHour) * DeterministicNoise.Sample(tick.Seed, stream ^ 7, day);

        if (_remainingKwh <= 0 && day > _lastPlugDay && hour >= plugInHour)
        {
            _lastPlugDay = day;
            _remainingKwh = profile.SessionMinKwh
                + (profile.SessionMaxKwh - profile.SessionMinKwh) * DeterministicNoise.Sample(tick.Seed, stream ^ 13, day);
        }

        if (_remainingKwh <= 0) return Kilowatts.Zero;
        if (hour >= profile.DepartureHour && hour < plugInHour) { _remainingKwh = 0; return Kilowatts.Zero; } // drove off

        var deliveredKwh = Math.Min(asset.RatedPowerKw * tick.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / tick.Duration.TotalHours);
    }
}

/// <summary>
/// Public charge point (A-004): seeded arrivals with a midday and evening peak,
/// sessions of 10-40 kWh. A busy point rejects arrivals - there is no queue.
/// </summary>
public sealed class PublicChargerBehaviour(ulong stream, PublicChargerProfile profile) : IAssetBehaviour
{
    private double _remainingKwh;

    public bool Busy => _remainingKwh > 0;

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        if (_remainingKwh <= 0)
        {
            var probability = profile.ArrivalsPerHour(tick.Instant.TimeOfDay.TotalHours) * tick.Duration.TotalHours;
            if (DeterministicNoise.Sample(tick.Seed, stream ^ 17, tick.TickIndex) >= probability) return Kilowatts.Zero;
            _remainingKwh = profile.SessionMinKwh
                + (profile.SessionMaxKwh - profile.SessionMinKwh) * DeterministicNoise.Sample(tick.Seed, stream ^ 31, tick.TickIndex);
        }

        var deliveredKwh = Math.Min(asset.RatedPowerKw * tick.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / tick.Duration.TotalHours);
    }

}
