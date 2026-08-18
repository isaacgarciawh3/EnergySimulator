namespace Sim.Simulation.Domain;

/// <summary>
/// Deterministic noise: the same (seed, stream, point) always yields the same
/// value in [0, 1). This is a HASH, not a random generator - it keeps no state
/// and does not care about call order, which is why adding an asset never
/// shifts another asset's sequence (ADR-0006). The pipeline: combine the three
/// inputs into one integer, scramble its bits with the published SplitMix64
/// finaliser, squash the top 53 bits into a double. The named constants are the
/// algorithm's published values (SplitMix64; FNV-1a for deriving streams from
/// identity strings) - they are not tunable and must never be "cleaned up".
/// </summary>
public static class DeterministicNoise
{
    private const ulong GoldenRatioIncrement = 0x9E3779B97F4A7C15UL;
    private const ulong SplitMix64MixerA = 0xBF58476D1CE4E5B9UL;
    private const ulong SplitMix64MixerB = 0x94D049BB133111EBUL;
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;
    private const int DoubleMantissaBits = 53;

    private static ulong CombineTheInputs(ulong seed, ulong stream, long point) =>
        seed ^ (stream * GoldenRatioIncrement) ^ (unchecked((ulong)point) * SplitMix64MixerA);

    private static ulong ScrambleTheBits(ulong bits)
    {
        bits ^= bits >> 30;
        bits *= SplitMix64MixerA;
        bits ^= bits >> 27;
        bits *= SplitMix64MixerB;
        bits ^= bits >> 31;
        return bits;
    }

    private static double SquashIntoTheUnitInterval(ulong bits) =>
        (bits >> (64 - DoubleMantissaBits)) * (1.0 / (1UL << DoubleMantissaBits));

    public static double Sample(ulong seed, ulong stream, long point) =>
        SquashIntoTheUnitInterval(ScrambleTheBits(CombineTheInputs(seed, stream, point)));

    public static ulong DeriveStreamFrom(string identity)
    {
        var hash = FnvOffsetBasis;
        foreach (var character in identity)
        {
            hash ^= character;
            hash *= FnvPrime;
        }
        return hash;
    }
}
