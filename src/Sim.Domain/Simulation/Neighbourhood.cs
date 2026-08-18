using Sim.Domain.Contracts;
using Sim.Domain.Simulation.Assets;

namespace Sim.Domain.Simulation;

/// <summary>
/// Aggregate root of the simulation. Advancing a tick measures every asset in a
/// FIXED order (floating-point addition is not associative — fixed-order
/// reduction is what keeps the run deterministic), then settles with the grid:
/// net consumption imports, net surplus exports, never both (RNF energy
/// conservation: generation + import == consumption + export).
/// </summary>
public sealed class Neighbourhood
{
    public const int RequiredHouses = 30;
    public const int RequiredPublicChargers = 6;

    private readonly List<IEnergyAsset> _measurementOrder;

    public Neighbourhood(IReadOnlyList<House> houses, IReadOnlyList<PublicEvCharger> publicChargers)
    {
        if (houses.Count != RequiredHouses)
            throw new ArgumentException($"Exactly {RequiredHouses} houses required, got {houses.Count}.", nameof(houses));
        if (publicChargers.Count != RequiredPublicChargers)
            throw new ArgumentException($"Exactly {RequiredPublicChargers} public chargers required, got {publicChargers.Count}.", nameof(publicChargers));

        Houses = houses;
        PublicChargers = publicChargers;
        _measurementOrder = houses.SelectMany(h => h.Assets).Concat(publicChargers).ToList();
    }

    public IReadOnlyList<House> Houses { get; }
    public IReadOnlyList<PublicEvCharger> PublicChargers { get; }

    public TickReport Advance(TickContext ctx)
    {
        var readings = new List<MeterReading>(_measurementOrder.Count);
        double consumption = 0, generation = 0;

        foreach (var asset in _measurementOrder)
        {
            var power = asset.Measure(ctx);
            readings.Add(new MeterReading(asset.MeterId, asset.OwnerId, asset.Type, ctx.Instant, power, power.For(ctx.Duration)));
            if (power.Value >= 0) consumption += power.Value;
            else generation -= power.Value;
        }

        var net = consumption - generation;
        var import = new Kilowatts(Math.Max(0, net));
        var export = new Kilowatts(Math.Max(0, -net));
        var grid = new GridFlow(new Kilowatts(net), import, export, import.For(ctx.Duration), export.For(ctx.Duration));
        var weather = new WeatherReport(ctx.Weather.TemperatureC, ctx.Weather.CloudCover, ctx.Weather.IrradianceFactor, ctx.Weather.Season.ToString());
        return new TickReport(ctx.TickIndex, ctx.Instant, ctx.Duration, readings, grid, weather);
    }
}
