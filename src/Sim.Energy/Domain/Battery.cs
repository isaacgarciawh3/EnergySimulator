namespace Sim.Energy.Domain;

/// <summary>
/// The neighbourhood battery, as a physical thing: how much it holds, how fast
/// it can move energy, and how much it loses on the round trip.
///
/// Nameplate data only. What it is doing right now is a control decision plus a
/// measurement, neither of which belongs here.
/// </summary>
public sealed record Battery(
    string MeterId,
    double CapacityKwh,
    double MaxPowerKw,
    double RoundTripEfficiency = 0.90)
{
    public static readonly Battery Default = new("neighbourhood/battery", CapacityKwh: 250, MaxPowerKw: 80);
}
