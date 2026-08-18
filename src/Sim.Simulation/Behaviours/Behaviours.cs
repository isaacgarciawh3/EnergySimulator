using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;

namespace Sim.Simulation.Behaviours;

/// <summary>Always-present household consumption (A-008): baseline shaped by a morning and evening curve with deterministic jitter.</summary>
public sealed class BaseLoadBehaviour(ulong stream) : IAssetBehaviour
{
    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var jitter = 0.9 + 0.2 * DeterministicNoise.Sample(tick.Seed, stream, tick.TickIndex);
        return new Kilowatts(asset.RatedPowerKw * DailyShape(tick.Instant.TimeOfDay.TotalHours) * jitter);
    }

    private static double DailyShape(double hour) => hour switch
    {
        < 6 => 0.55,   // night trough
        < 9 => 1.5,    // morning peak
        < 17 => 0.9,   // daytime
        < 22 => 1.8,   // evening peak
        _ => 0.8,
    };
}

/// <summary>Rooftop PV: capacity scaled by the interval's irradiance. Negative because it generates.</summary>
public sealed class PvBehaviour : IAssetBehaviour
{
    public Kilowatts PowerAt(Asset asset, SimulationTick tick) =>
        new(-asset.RatedPowerKw * tick.Weather.IrradianceFactor);
}

/// <summary>Balance-point heat pump (A-005): draw rises linearly below 15 C, capped at rated power.</summary>
public sealed class HeatPumpBehaviour(ulong stream) : IAssetBehaviour
{
    public const double BalancePointC = 15.0;

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var deficit = Math.Max(0.0, BalancePointC - tick.Weather.TemperatureC);
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
public sealed class HomeEvChargerBehaviour(ulong stream) : IAssetBehaviour
{
    private double _remainingKwh;
    private long _lastPlugDay = long.MinValue;

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        var hour = tick.Instant.TimeOfDay.TotalHours;
        var day = tick.Instant.UtcTicks / TimeSpan.TicksPerDay;
        var plugInHour = 17.5 + 1.5 * DeterministicNoise.Sample(tick.Seed, stream ^ 7, day);

        if (_remainingKwh <= 0 && day > _lastPlugDay && hour >= plugInHour)
        {
            _lastPlugDay = day;
            _remainingKwh = 8.0 + 4.0 * DeterministicNoise.Sample(tick.Seed, stream ^ 13, day);
        }

        if (_remainingKwh <= 0) return Kilowatts.Zero;
        if (hour >= 7.0 && hour < plugInHour) { _remainingKwh = 0; return Kilowatts.Zero; } // drove off

        var deliveredKwh = Math.Min(asset.RatedPowerKw * tick.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / tick.Duration.TotalHours);
    }
}

/// <summary>
/// Public charge point (A-004): seeded arrivals with a midday and evening peak,
/// sessions of 10-40 kWh. A busy point rejects arrivals - there is no queue.
/// </summary>
public sealed class PublicChargerBehaviour(ulong stream) : IAssetBehaviour
{
    private double _remainingKwh;

    public bool Busy => _remainingKwh > 0;

    public Kilowatts PowerAt(Asset asset, SimulationTick tick)
    {
        if (_remainingKwh <= 0)
        {
            var probability = ArrivalsPerHour(tick.Instant.TimeOfDay.TotalHours) * tick.Duration.TotalHours;
            if (DeterministicNoise.Sample(tick.Seed, stream ^ 17, tick.TickIndex) >= probability) return Kilowatts.Zero;
            _remainingKwh = 10.0 + 30.0 * DeterministicNoise.Sample(tick.Seed, stream ^ 31, tick.TickIndex);
        }

        var deliveredKwh = Math.Min(asset.RatedPowerKw * tick.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / tick.Duration.TotalHours);
    }

    private static double ArrivalsPerHour(double hour) => hour switch
    {
        < 6 => 0.05,
        < 10 => 0.20,
        < 15 => 0.35,
        < 21 => 0.45,
        _ => 0.10,
    };
}
