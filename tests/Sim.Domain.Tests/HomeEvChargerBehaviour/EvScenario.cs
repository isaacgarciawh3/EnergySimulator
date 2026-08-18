using Sim.Energy.Domain;
using Sim.Simulation.Behaviours;
using Sim.Simulation.Domain;
using Sim.Simulation.Parameters;

namespace Sim.Domain.Tests.HomeEvChargerBehaviourScenario;

/// <summary>
/// Shared vocabulary. The profile has ZERO-WIDTH windows (SessionMin equals
/// SessionMax, PlugInFrom equals PlugInTo), so every draw is exact and no test
/// reverse-engineers noise: plug-in at 18:00 sharp, 10 kWh sharp, departure 07:00.
/// </summary>
internal static class EvScenario
{
    public static readonly Asset Charger = new("house-01/ev-charger", "house-01", AssetType.HomeEvCharger, 7.4);

    public static HomeChargerProfile ExactProfile => new(
        SessionMinKwh: 10, SessionMaxKwh: 10, PlugInFromHour: 18, PlugInToHour: 18, DepartureHour: 7);

    public static HomeEvChargerBehaviour Behaviour() => new(stream: 1, ExactProfile);

    public static SimulationTick TickAt(int day, double hour) => new(
        TickIndex: (long)((day * 24 + hour) * 4),
        Instant: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(day).AddHours(hour),
        Duration: TimeSpan.FromMinutes(15),
        Weather: new WeatherConditions(10, 0, 0, Season.Winter),
        Seed: 42);
}
