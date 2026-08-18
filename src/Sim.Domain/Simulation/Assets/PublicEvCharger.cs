using Sim.Domain.Contracts;

namespace Sim.Domain.Simulation.Assets;

/// <summary>
/// Public charging point (A-004): seeded arrivals following a time-of-day rate
/// (midday and evening peaks); a session needs 10–40 kWh at 11 kW. A busy
/// charger rejects new arrivals — no queueing (documented simplification).
/// </summary>
public sealed class PublicEvCharger(string id) : EnergyAssetBase(id, "meter", AssetType.PublicEvCharger)
{
    public const double PowerKw = 11.0;

    private double _remainingKwh;

    public bool Busy => _remainingKwh > 0;

    public override Kilowatts Measure(TickContext ctx)
    {
        if (_remainingKwh <= 0)
        {
            var arrivalProbability = ArrivalsPerHour(ctx.Instant.TimeOfDay.TotalHours) * ctx.Duration.TotalHours;
            if (Noise(ctx, salt: 17) >= arrivalProbability) return Kilowatts.Zero;
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
