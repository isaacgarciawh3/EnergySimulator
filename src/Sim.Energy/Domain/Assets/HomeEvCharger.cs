using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain.Assets;

/// <summary>
/// Home EV charging (A-004): one seeded plug-in per day between 17:30 and 19:00
/// needing 8-12 kWh at 7.4 kW, charging until full or until the 07:00 departure.
/// Reported power is the tick average, so the final partial interval accounts
/// for exactly the energy delivered.
/// </summary>
public sealed class HomeEvCharger(string ownerId) : EnergyAssetBase(ownerId, "ev-charger", AssetType.HomeEvCharger)
{
    public const double PowerKw = 7.4;

    private double _remainingKwh;
    private long _lastPlugDay = long.MinValue;

    public override Kilowatts Measure(MeasurementContext ctx)
    {
        var hour = ctx.Instant.TimeOfDay.TotalHours;
        var day = ctx.Instant.UtcTicks / TimeSpan.TicksPerDay;
        var plugInHour = 17.5 + 1.5 * PerDayNoise(ctx, salt: 7, day);

        if (_remainingKwh <= 0 && day > _lastPlugDay && hour >= plugInHour)
        {
            _lastPlugDay = day;
            _remainingKwh = 8.0 + 4.0 * PerDayNoise(ctx, salt: 13, day);
        }

        if (_remainingKwh <= 0) return Kilowatts.Zero;
        if (hour >= 7.0 && hour < plugInHour) { _remainingKwh = 0; return Kilowatts.Zero; } // drove off

        var deliveredKwh = Math.Min(PowerKw * ctx.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / ctx.Duration.TotalHours);
    }
}
