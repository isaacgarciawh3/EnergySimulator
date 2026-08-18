using Sim.Energy.Domain;
using Sim.Simulation.Behaviours;
using Sim.Simulation.Domain;
using Sim.Simulation.Parameters;

namespace Sim.Domain.Tests.PublicChargerBehaviourScenario;

/// <summary>
/// Shared vocabulary. Arrival rates are CERTAIN (4/h x 15 min = probability 1)
/// or NEVER (0), and the session has zero width (11 kWh sharp at 11 kW rated,
/// exactly one hour), so every scenario is deterministic without touching noise.
/// </summary>
internal static class PublicScenario
{
    public static readonly Asset Point = new("public-charger-1/meter", "public-charger-1", AssetType.PublicEvCharger, 11.0);

    public static PublicChargerProfile CertainBeforeDawn => new(
        SessionMinKwh: 11, SessionMaxKwh: 11, ArrivalsPerHourByBand: [4.0, 0.0], BandUpperHours: [6, 24]);

    public static PublicChargerProfile NeverAnyone => new(
        SessionMinKwh: 11, SessionMaxKwh: 11, ArrivalsPerHourByBand: [0.0], BandUpperHours: [24]);

    public static PublicChargerBehaviour BehaviourWith(PublicChargerProfile profile) => new(stream: 1, profile);

    public static SimulationTick TickAt(double hour, long index) => new(
        TickIndex: index,
        Instant: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddHours(hour),
        Duration: TimeSpan.FromMinutes(15),
        Weather: new WeatherConditions(10, 0, 0, Season.Winter),
        Seed: 42);
}
