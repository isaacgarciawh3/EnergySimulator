namespace Sim.Energy.Domain;

/// <summary>
/// The rules the assignment states as absolute, written so they can be read as
/// sentences and enforced in one place.
///
/// These are INVARIANTS, not settings. A configuration file supplies the values
/// the neighbourhood is built from, but it can never talk the neighbourhood out
/// of these: an invalid neighbourhood is not representable, because the
/// constructor refuses to produce one.
/// </summary>
public static class NeighbourhoodInvariants
{
    public const int RequiredHouses = 30;
    public const int RequiredPublicChargers = 6;

    public static void TheNeighbourhoodMustHaveExactlyThirtyHouses(IReadOnlyList<House> houses)
    {
        if (houses.Count != RequiredHouses)
            throw new NeighbourhoodInvariantViolation(
                $"The neighbourhood must have exactly {RequiredHouses} houses, but {houses.Count} were supplied.");
    }

    public static void TheNeighbourhoodMustHaveExactlySixPublicChargers(IReadOnlyList<Asset> chargePoints)
    {
        if (chargePoints.Count != RequiredPublicChargers)
            throw new NeighbourhoodInvariantViolation(
                $"The neighbourhood must have exactly {RequiredPublicChargers} public charge points, but {chargePoints.Count} were supplied.");
    }

    public static void EveryPublicChargePointMustBeAPublicCharger(IReadOnlyList<Asset> chargePoints)
    {
        var wrong = chargePoints.FirstOrDefault(a => a.Type != AssetType.PublicEvCharger);
        if (wrong is not null)
            throw new NeighbourhoodInvariantViolation(
                $"Public charge point '{wrong.MeterId}' must be of type {AssetType.PublicEvCharger}, but was {wrong.Type}.");
    }

    public static void EveryHouseMustHaveBaseHouseholdConsumption(string houseId, IReadOnlyList<Asset> assets)
    {
        if (!assets.Any(a => a.Type == AssetType.BaseLoad))
            throw new NeighbourhoodInvariantViolation(
                $"House '{houseId}' must always have base household consumption, but no {AssetType.BaseLoad} asset was supplied.");
    }

    public static void EveryHouseMustHaveAtMostOneOfEachAssetType(string houseId, IReadOnlyList<Asset> assets)
    {
        var duplicate = assets.GroupBy(a => a.Type).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new NeighbourhoodInvariantViolation(
                $"House '{houseId}' has {duplicate.Count()} assets of type {duplicate.Key}, but at most one is allowed.");
    }

    public static void EveryMeterMustBeUniquelyIdentified(IReadOnlyList<Asset> allAssets, Battery? battery)
    {
        var ids = allAssets.Select(a => a.MeterId).ToList();
        if (battery is not null) ids.Add(battery.MeterId);

        var duplicate = ids.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new NeighbourhoodInvariantViolation(
                $"Meter id '{duplicate.Key}' is used {duplicate.Count()} times; every meter must be uniquely identified.");
    }

    public static void EveryAssetMustHaveANonNegativeRating(IReadOnlyList<Asset> allAssets)
    {
        var invalid = allAssets.FirstOrDefault(a => a.RatedPowerKw < 0);
        if (invalid is not null)
            throw new NeighbourhoodInvariantViolation(
                $"Asset '{invalid.MeterId}' has a negative rating of {invalid.RatedPowerKw} kW; ratings are magnitudes and cannot be negative.");
    }
}

/// <summary>Raised when a neighbourhood would violate a rule the assignment states as absolute.</summary>
public sealed class NeighbourhoodInvariantViolation(string message) : InvalidOperationException(message);
