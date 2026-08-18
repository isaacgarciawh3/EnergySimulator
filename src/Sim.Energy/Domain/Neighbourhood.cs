namespace Sim.Energy.Domain;

/// <summary>
/// AGGREGATE ROOT of the Energy context. It describes the physical world:
/// which houses exist, what is installed in them, which meters those assets sit
/// behind, and the six shared charge points.
///
/// It has no behaviour beyond protecting its invariants. It does not know what
/// the weather is, what time it is, or how much power anything is drawing -
/// those belong to whoever produces readings.
/// </summary>
public sealed class Neighbourhood
{
    public const int RequiredHouses = 30;
    public const int RequiredPublicChargers = 6;

    public Neighbourhood(IReadOnlyList<House> houses, IReadOnlyList<Asset> publicChargePoints, Battery? battery = null)
    {
        if (houses.Count != RequiredHouses)
            throw new ArgumentException($"Exactly {RequiredHouses} houses required, got {houses.Count}.", nameof(houses));
        if (publicChargePoints.Count != RequiredPublicChargers)
            throw new ArgumentException($"Exactly {RequiredPublicChargers} public charge points required, got {publicChargePoints.Count}.", nameof(publicChargePoints));
        if (publicChargePoints.Any(a => a.Type != AssetType.PublicEvCharger))
            throw new ArgumentException("Public charge points must all be of type PublicEvCharger.", nameof(publicChargePoints));

        Houses = houses;
        PublicChargePoints = publicChargePoints;
        Battery = battery;
        // Fixed order: floating point addition is not associative, so a stable
        // enumeration order is what keeps aggregate results reproducible.
        AllAssets = houses.SelectMany(h => h.Assets).Concat(publicChargePoints).ToList();
    }

    public IReadOnlyList<House> Houses { get; }
    public IReadOnlyList<Asset> PublicChargePoints { get; }
    public IReadOnlyList<Asset> AllAssets { get; }

    /// <summary>Optional shared storage. Excluded from AllAssets because it is commanded, not simulated from the weather.</summary>
    public Battery? Battery { get; }

    public AssetType TypeOf(string meterId) =>
        AllAssets.First(a => a.MeterId == meterId).Type;
}
