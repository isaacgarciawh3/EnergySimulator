using Sim.Energy.Domain;
using Sim.SharedKernel;

namespace Sim.Domain.Tests;

/// <summary>
/// Fixtures and floating point tolerances shared by the domain tests.
///
/// TOLERANCE POLICY (stated once, used everywhere):
/// every comparison of two computed doubles uses <see cref="Close(double, double)"/>,
/// which allows the LARGER of an absolute 1e-6 kW/kWh and a relative 1e-9 of the
/// magnitudes involved. The absolute floor exists because a quantity that should
/// be zero has no magnitude to be relative to; the relative term exists because
/// summing a few thousand readings of a few hundred kW loses the last bits of a
/// double. Exact equality is asserted ONLY where the arithmetic is provably exact
/// in binary floating point (4 kW x 0.25 h = 1 kWh) or where the claim IS
/// bit-for-bit reproducibility (determinism).
/// </summary>
internal static class TestWorld
{
    public const double AbsoluteTolerance = 1e-6;
    public const double RelativeTolerance = 1e-9;

    public static readonly DateTimeOffset Instant = new(2026, 1, 15, 18, 0, 0, TimeSpan.Zero);
    public static readonly TimeSpan Quarter = TimeSpan.FromMinutes(15);

    /// <summary>Tolerance appropriate to the magnitudes being compared. See the class remarks.</summary>
    public static double Close(double a, double b) =>
        Math.Max(AbsoluteTolerance, RelativeTolerance * Math.Max(Math.Abs(a), Math.Abs(b)));

    public static House HouseWithBaseLoad(string id) =>
        new(id, [new Asset($"{id}/base", id, AssetType.BaseLoad, 0.4)]);

    public static IReadOnlyList<House> Houses(int count) =>
        Enumerable.Range(1, count).Select(i => HouseWithBaseLoad($"house-{i:00}")).ToList();

    public static IReadOnlyList<Asset> ChargePoints(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Asset($"public-charger-{i}/meter", $"public-charger-{i}", AssetType.PublicEvCharger, 11.0))
            .ToList();

    /// <summary>
    /// Maps arbitrary ints from FsCheck onto plausible signed power values in
    /// [-1000, +1000] kW at 10 W resolution. Doing the mapping by hand rather
    /// than generating raw doubles keeps NaN and infinity out of the ledger,
    /// which are not readings any meter can produce.
    /// </summary>
    public static IReadOnlyList<PowerReading> ReadingsFrom(int[]? centiKilowatts) =>
        (centiKilowatts ?? [])
            .Select((c, i) => new PowerReading($"meter-{i}", Instant, new Kilowatts(c % 100_000 / 100.0)))
            .ToList();
}
