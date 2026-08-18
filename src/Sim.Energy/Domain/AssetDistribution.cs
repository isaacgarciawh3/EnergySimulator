namespace Sim.Energy.Domain;

/// <summary>
/// The share of houses carrying each optional asset. The assignment requires the
/// distribution to be DOCUMENTED, so the neighbourhood is able to state its own
/// distribution rather than leaving it as a claim in a README that may drift.
/// </summary>
public sealed record AssetDistribution(int Houses, int WithPv, int WithHeatPump, int WithHomeEvCharger)
{
    public double PvShare => Share(WithPv);
    public double HeatPumpShare => Share(WithHeatPump);
    public double HomeEvShare => Share(WithHomeEvCharger);

    private double Share(int count) => Houses == 0 ? 0 : (double)count / Houses;

    public static AssetDistribution Of(IReadOnlyList<House> houses) => new(
        houses.Count,
        houses.Count(h => h.Has(AssetType.Pv)),
        houses.Count(h => h.Has(AssetType.HeatPump)),
        houses.Count(h => h.Has(AssetType.HomeEvCharger)));

    public override string ToString() =>
        $"{Houses} houses: {PvShare:P0} PV, {HeatPumpShare:P0} heat pump, {HomeEvShare:P0} home EV charger";
}
