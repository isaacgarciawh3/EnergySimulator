using Sim.Energy.Domain.Assets;
using Sim.SharedKernel;

namespace Sim.Energy.Domain;

/// <summary>Documented asset distribution (A-006). Shares are independent per-house probabilities, so a house may hold several assets.</summary>
public sealed record NeighbourhoodBlueprint(double PvShare, double HeatPumpShare, double HomeEvShare)
{
    public static readonly NeighbourhoodBlueprint Default = new(PvShare: 0.4, HeatPumpShare: 0.3, HomeEvShare: 0.2);
}

/// <summary>Builds the neighbourhood as a pure function of (seed, blueprint) — the root of reproducibility.</summary>
public static class NeighbourhoodFactory
{
    private const ulong Baseline = 1, PvDraw = 2, PvSize = 3, HpDraw = 4, HpSize = 5, EvDraw = 6;

    public static Neighbourhood Create(ulong seed, NeighbourhoodBlueprint blueprint)
    {
        var houses = new List<House>(Neighbourhood.RequiredHouses);
        for (var i = 1; i <= Neighbourhood.RequiredHouses; i++)
        {
            var id = $"house-{i:00}";
            var assets = new List<IEnergyAsset>
            {
                new BaseLoad(id, 0.2 + 0.4 * DeterministicNoise.Sample(seed, Baseline, i)),
            };
            if (DeterministicNoise.Sample(seed, PvDraw, i) < blueprint.PvShare)
                assets.Add(new PvArray(id, 3.0 + 5.0 * DeterministicNoise.Sample(seed, PvSize, i)));
            if (DeterministicNoise.Sample(seed, HpDraw, i) < blueprint.HeatPumpShare)
                assets.Add(new HeatPump(id, 0.10 + 0.05 * DeterministicNoise.Sample(seed, HpSize, i), maxKw: 3.0));
            if (DeterministicNoise.Sample(seed, EvDraw, i) < blueprint.HomeEvShare)
                assets.Add(new HomeEvCharger(id));
            houses.Add(new House(id, assets));
        }

        var chargers = Enumerable.Range(1, Neighbourhood.RequiredPublicChargers)
            .Select(i => new PublicEvCharger($"public-charger-{i}")).ToList();

        return new Neighbourhood(houses, chargers);
    }
}
