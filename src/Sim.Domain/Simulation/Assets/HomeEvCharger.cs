using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation.Assets;

/// <summary>
/// Home EV charging (A-004): one seeded plug-in per day in the 17:30–19:00
/// window, needing 8–12 kWh, charging at 7.4 kW until full or 07:00 departure.
/// Reported power is the tick average, so energy accounting is exact even on
/// the final partial tick of a session.
/// </summary>
public sealed class HomeEvCharger(string ownerId) : EnergyAssetBase(ownerId, "ev-charger", AssetType.HomeEvCharger)
{
    public const double PowerKw = 7.4;

    private double _remainingKwh;
    private long _lastPlugDay = long.MinValue;

    public override Kilowatts Measure(TickContext ctx)
    {
        var hour = ctx.Instant.TimeOfDay.TotalHours;
        var day = ctx.Instant.UtcTicks / TimeSpan.TicksPerDay;
        var plugInHour = 17.5 + 1.5 * DailyNoise(ctx, salt: 7, day);

        if (_remainingKwh <= 0 && day > _lastPlugDay && hour >= plugInHour)
        {
            _lastPlugDay = day;
            _remainingKwh = 8.0 + 4.0 * DailyNoise(ctx, salt: 13, day);
        }

        if (_remainingKwh <= 0) return Kilowatts.Zero;
        if (hour is >= 7.0 && hour < plugInHour) { _remainingKwh = 0; return Kilowatts.Zero; } // departure

        var deliveredKwh = Math.Min(PowerKw * ctx.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / ctx.Duration.TotalHours);
    }
}
