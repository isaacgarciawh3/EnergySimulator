using Sim.Energy.Domain;
using Sim.Simulation.Domain;

namespace Sim.Application.Configuration;

/// <summary>
/// Builds the physical world from configuration. This sits in the application
/// layer, not in Energy: Energy describes a neighbourhood, it does not decide
/// which houses got solar panels. That is a configuration concern, and here it
/// happens to be answered by a seed.
/// </summary>
public static class NeighbourhoodBuilder
{
    private const ulong Baseline = 1, PvDraw = 2, PvSize = 3, HpDraw = 4, HpSize = 5, EvDraw = 6;

    public static Neighbourhood Build(SimulationConfiguration configuration, SimulationParameters? parameters = null)
    {
        var p = parameters ?? new SimulationParameters();
        var seed = unchecked((ulong)configuration.Seed);
        var houses = new List<House>(Neighbourhood.RequiredHouses);

        for (var i = 1; i <= Neighbourhood.RequiredHouses; i++)
        {
            var id = $"house-{i:00}";
            var assets = new List<Asset>
            {
                new($"{id}/base", id, AssetType.BaseLoad,
                    p.BaseLoadKw.Min + p.BaseLoadKw.Spread * DeterministicNoise.Sample(seed, Baseline, i)),
            };
            if (DeterministicNoise.Sample(seed, PvDraw, i) < configuration.PvShare)
                assets.Add(new Asset($"{id}/pv", id, AssetType.Pv,
                    p.PvCapacityKwp.Min + p.PvCapacityKwp.Spread * DeterministicNoise.Sample(seed, PvSize, i)));
            if (DeterministicNoise.Sample(seed, HpDraw, i) < configuration.HeatPumpShare)
                assets.Add(new Asset($"{id}/heat-pump", id, AssetType.HeatPump, p.HeatPump.MaxKw,
                    ResponseCoefficient: p.HeatPump.KwPerDegree.Min
                        + p.HeatPump.KwPerDegree.Spread * DeterministicNoise.Sample(seed, HpSize, i)));
            if (DeterministicNoise.Sample(seed, EvDraw, i) < configuration.HomeEvShare)
                assets.Add(new Asset($"{id}/ev-charger", id, AssetType.HomeEvCharger, p.HomeCharger.PowerKw));
            houses.Add(new House(id, assets));
        }

        var chargePoints = Enumerable.Range(1, Neighbourhood.RequiredPublicChargers)
            .Select(i => new Asset($"public-charger-{i}/meter", $"public-charger-{i}", AssetType.PublicEvCharger, p.PublicCharger.PowerKw))
            .ToList();

        var battery = configuration.BatteryEnabled
            ? new Battery("neighbourhood/battery", configuration.BatteryCapacityKwh,
                configuration.BatteryMaxPowerKw, configuration.BatteryRoundTripEfficiency)
            : null;

        return new Neighbourhood(houses, chargePoints, battery);
    }
}
