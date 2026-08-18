namespace Sim.Energy.Domain;

/// <summary>
/// AGGREGATE ROOT of the Energy context: describes the physical world - which
/// houses exist, what sits behind which meter, the six public charge points and
/// the optional shared battery - and refuses to exist in any shape that
/// violates the assignment's absolutes. It has no behaviour: what an asset is
/// doing right now is the Simulation's question. AllAssets enumerates in a
/// FIXED order, because floating point addition is not associative and a stable
/// order is what keeps every aggregate result reproducible. Distribution states
/// the ACTUAL asset spread, so the documented figure cannot drift from reality.
/// </summary>
public sealed class Neighbourhood
{
    public const int RequiredHouses = 30;
    public const int RequiredPublicChargers = 6;

    public Neighbourhood(IReadOnlyList<House> houses, IReadOnlyList<Asset> publicChargePoints, Battery? battery = null)
    {
        RefuseUnlessThereAreExactlyThirtyHouses(houses);
        RefuseUnlessThereAreExactlySixChargePoints(publicChargePoints);
        RefuseUnlessEveryChargePointIsPublic(publicChargePoints);

        Houses = houses;
        PublicChargePoints = publicChargePoints;
        Battery = battery;
        AllAssets = houses.SelectMany(h => h.Assets).Concat(publicChargePoints).ToList();

        RefuseUnlessEveryMeterIsUnique(AllAssets, battery);
        RefuseUnlessEveryRatingIsNonNegative(AllAssets);

        Distribution = AssetDistribution.Of(houses);
    }

    private static void RefuseUnlessThereAreExactlyThirtyHouses(IReadOnlyList<House> houses)
    {
        if (houses.Count != RequiredHouses)
            throw new NeighbourhoodInvariantViolation(
                $"The neighbourhood must have exactly {RequiredHouses} houses, but {houses.Count} were supplied.");
    }

    private static void RefuseUnlessThereAreExactlySixChargePoints(IReadOnlyList<Asset> chargePoints)
    {
        if (chargePoints.Count != RequiredPublicChargers)
            throw new NeighbourhoodInvariantViolation(
                $"The neighbourhood must have exactly {RequiredPublicChargers} public charge points, but {chargePoints.Count} were supplied.");
    }

    private static void RefuseUnlessEveryChargePointIsPublic(IReadOnlyList<Asset> chargePoints)
    {
        var impostor = chargePoints.FirstOrDefault(a => a.Type != AssetType.PublicEvCharger);
        if (impostor is not null)
            throw new NeighbourhoodInvariantViolation(
                $"Public charge point '{impostor.MeterId}' must be of type {AssetType.PublicEvCharger}, but was {impostor.Type}.");
    }

    private static void RefuseUnlessEveryMeterIsUnique(IReadOnlyList<Asset> allAssets, Battery? battery)
    {
        var ids = allAssets.Select(a => a.MeterId).ToList();
        if (battery is not null) ids.Add(battery.MeterId);

        var duplicate = ids.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new NeighbourhoodInvariantViolation(
                $"Meter id '{duplicate.Key}' is used {duplicate.Count()} times; every meter must be uniquely identified.");
    }

    private static void RefuseUnlessEveryRatingIsNonNegative(IReadOnlyList<Asset> allAssets)
    {
        var invalid = allAssets.FirstOrDefault(a => a.RatedPowerKw < 0);
        if (invalid is not null)
            throw new NeighbourhoodInvariantViolation(
                $"Asset '{invalid.MeterId}' has a negative rating of {invalid.RatedPowerKw} kW; ratings are magnitudes and cannot be negative.");
    }

    public IReadOnlyList<House> Houses { get; }
    public IReadOnlyList<Asset> PublicChargePoints { get; }
    public IReadOnlyList<Asset> AllAssets { get; }
    public Battery? Battery { get; }
    public AssetDistribution Distribution { get; }

    public AssetType TypeOf(string meterId) => AllAssets.First(a => a.MeterId == meterId).Type;
}

/// <summary>Raised when a rule of the Energy context would be violated. One type for the whole context: the message names the rule.</summary>
public sealed class NeighbourhoodInvariantViolation(string message) : InvalidOperationException(message);
