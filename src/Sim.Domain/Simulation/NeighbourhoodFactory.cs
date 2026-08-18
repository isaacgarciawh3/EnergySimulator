using Sim.Domain.Simulation.Assets;

namespace Sim.Domain.Simulation;

/// <summary>Documented asset distribution across houses (A-006). Shares are probabilities per independent seeded draw, so overlaps are expected.</summary>
public sealed record NeighbourhoodBlueprint(double PvShare, double HeatPumpShare, double HomeEvShare)
{
    public static readonly NeighbourhoodBlueprint Default = new(PvShare: 0.4, HeatPumpShare: 0.3, HomeEvShare: 0.2);
}

/// <summary>
/// Builds the neighbourhood as a pure function of (seed, blueprint): same seed,
/// same layout, same parameters — the root of reproducibility (RNF determinism).
/// </summary>
public static class NeighbourhoodFactory
{
    private const ulong BaselineStream = 1;
    private const ulong PvDrawStream = 2;
    private const ulong PvSizeStream = 3;
    private const ulong HeatPumpDrawStream = 4;
    private const ulong HeatPumpSizeStream = 5;
    private const ulong EvDrawStream = 6;

    public static Neighbourhood Create(ulong seed, NeighbourhoodBlueprint blueprint)
    {
        var houses = new List<House>(Neighbourhood.RequiredHouses);
        for (var i = 1; i <= Neighbourhood.RequiredHouses; i++)
        {
            var id = $"house-{i:00}";
            var assets = new List<IEnergyAsset>
            {
                new BaseLoad(id, baselineKw: 0.2 + 0.4 * DeterministicNoise.Sample(seed, BaselineStream, i)),
            };
            if (DeterministicNoise.Sample(seed, PvDrawStream, i) < blueprint.PvShare)
                assets.Add(new PvArray(id, capacityKwp: 3.0 + 5.0 * DeterministicNoise.Sample(seed, PvSizeStream, i)));
            if (DeterministicNoise.Sample(seed, HeatPumpDrawStream, i) < blueprint.HeatPumpShare)
                assets.Add(new HeatPump(id, kwPerDegree: 0.10 + 0.05 * DeterministicNoise.Sample(seed, HeatPumpSizeStream, i), maxKw: 3.0));
            if (DeterministicNoise.Sample(seed, EvDrawStream, i) < blueprint.HomeEvShare)
                assets.Add(new HomeEvCharger(id));
            houses.Add(new House(id, assets));
        }

        var chargers = Enumerable.Range(1, Neighbourhood.RequiredPublicChargers)
            .Select(i => new PublicEvCharger($"public-charger-{i}"))
            .ToList();

        return new Neighbourhood(houses, chargers);
    }
}
