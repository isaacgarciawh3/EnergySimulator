using Sim.Application.Configuration;
using Sim.Simulation.Domain;

namespace Sim.Domain.Tests.WeatherModelScenario;

/// <summary>
/// Shared vocabulary for the weather scenarios. Every seasonal claim is checked
/// against a SWEEP of seeds, not one lucky one - the single-seed midday
/// comparison passes by coincidence and flips for 2 of these 12.
/// </summary>
internal static class WeatherScenario
{
    public static readonly ulong ConfiguredSeed = unchecked((ulong)SimulationConfiguration.Default.Seed);

    public static readonly ulong[] Seeds = [20260818, 1, 2, 3, 7, 42, 123, 999, 555, 31337, 2026, 88];

    public static DateTimeOffset SummerAt(double hour) =>
        new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromHours(hour);

    public static DateTimeOffset WinterAt(double hour) =>
        new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromHours(hour);

    /// <summary>Irradiance integrated over one day at the default 15 minute interval.</summary>
    public static double DailyIrradiance(WeatherModel model, Func<double, DateTimeOffset> day) =>
        Enumerable.Range(0, 96).Sum(slot => model.At(day(slot / 4.0)).IrradianceFactor) * 0.25;
}
