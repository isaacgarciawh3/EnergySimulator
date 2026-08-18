using Sim.Energy.Contracts;
using Sim.SharedKernel;

namespace Sim.Energy.Domain.Assets;

/// <summary>
/// Public charge point (A-004): seeded arrivals following a time-of-day rate
/// (midday and evening peaks), sessions of 10-40 kWh at 11 kW. A busy point
/// rejects arrivals — no queueing, a documented simplification.
/// </summary>
public sealed class PublicEvCharger(string id) : EnergyAssetBase(id, "meter", AssetType.PublicEvCharger)
{
    public const double PowerKw = 11.0;

    private double _remainingKwh;

    public bool Busy => _remainingKwh > 0;

    public override Kilowatts Measure(MeasurementContext ctx)
    {
        if (_remainingKwh <= 0)
        {
            var probability = ArrivalsPerHour(ctx.Instant.TimeOfDay.TotalHours) * ctx.Duration.TotalHours;
            if (Noise(ctx, salt: 17) >= probability) return Kilowatts.Zero;
            _remainingKwh = 10.0 + 30.0 * Noise(ctx, salt: 31);
        }

        var deliveredKwh = Math.Min(PowerKw * ctx.Duration.TotalHours, _remainingKwh);
        _remainingKwh -= deliveredKwh;
        return new Kilowatts(deliveredKwh / ctx.Duration.TotalHours);
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
