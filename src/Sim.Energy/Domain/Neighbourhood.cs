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
    public const int RequiredHouses = NeighbourhoodInvariants.RequiredHouses;
    public const int RequiredPublicChargers = NeighbourhoodInvariants.RequiredPublicChargers;

    public Neighbourhood(IReadOnlyList<House> houses, IReadOnlyList<Asset> publicChargePoints, Battery? battery = null)
    {
        NeighbourhoodInvariants.TheNeighbourhoodMustHaveExactlyThirtyHouses(houses);
        NeighbourhoodInvariants.TheNeighbourhoodMustHaveExactlySixPublicChargers(publicChargePoints);
        NeighbourhoodInvariants.EveryPublicChargePointMustBeAPublicCharger(publicChargePoints);

        Houses = houses;
        PublicChargePoints = publicChargePoints;
        Battery = battery;
        // Fixed order: floating point addition is not associative, so a stable
        // enumeration order is what keeps aggregate results reproducible.
        AllAssets = houses.SelectMany(h => h.Assets).Concat(publicChargePoints).ToList();

        NeighbourhoodInvariants.EveryMeterMustBeUniquelyIdentified(AllAssets, battery);
        NeighbourhoodInvariants.EveryAssetMustHaveANonNegativeRating(AllAssets);

        Distribution = AssetDistribution.Of(houses);
    }

    /// <summary>The neighbourhood states its own asset distribution, so the documented figure cannot drift from reality.</summary>
    public AssetDistribution Distribution { get; }

    public IReadOnlyList<House> Houses { get; }
    public IReadOnlyList<Asset> PublicChargePoints { get; }
    public IReadOnlyList<Asset> AllAssets { get; }

    /// <summary>Optional shared storage. Excluded from AllAssets because it is commanded, not simulated from the weather.</summary>
    public Battery? Battery { get; }

    public AssetType TypeOf(string meterId) =>
        AllAssets.First(a => a.MeterId == meterId).Type;
}
