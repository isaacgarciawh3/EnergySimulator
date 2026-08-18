using Sim.Accounting.Contracts;
using Sim.Energy.Contracts;
using Sim.Simulation.Contracts;

namespace Sim.Application.Translation;

/// <summary>
/// ANTI-CORRUPTION LAYER. The three bounded contexts share no types beyond the
/// physical units in the shared kernel, so somebody has to translate — and that
/// somebody is the application layer, never a domain.
///
/// Note what each translation DROPS: the Energy context never learns what a
/// season or a cloud is, and the Accounting context never learns what a heat
/// pump is. That narrowing is the point: it is what lets any context change its
/// internal model without breaking the others.
/// </summary>
public static class ContextTranslator
{
    public static MeasurementContext ToMeasurementContext(TickEnvironment env, ulong seed) =>
        new(env.TickIndex, env.Instant, env.Duration,
            new EnvironmentInfluence(env.TemperatureC, env.IrradianceFactor), seed);

    public static EnergyEntry ToEnergyEntry(MeterReading reading) =>
        new(reading.MeterId, reading.OwnerId, reading.Type.ToString(),
            KindOf(reading.Type), reading.Instant, reading.Power, reading.Energy);

    private static MeterKind KindOf(AssetType type) => type switch
    {
        AssetType.Pv => MeterKind.Generator,
        _ => MeterKind.Consumer,
    };
}
