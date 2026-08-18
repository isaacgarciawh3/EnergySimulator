namespace Sim.Domain.Simulation;

/// <summary>
/// Pure hash-based noise (SplitMix64 finalizer): the same (seed, stream, point)
/// always yields the same value in [0, 1). Stateless by design — determinism
/// does not depend on call order, which keeps replay and testing trivial.
/// </summary>
public static class DeterministicNoise
{
    public static double Sample(ulong seed, ulong stream, long point)
    {
        var x = seed ^ (stream * 0x9E3779B97F4A7C15UL) ^ (unchecked((ulong)point) * 0xBF58476D1CE4E5B9UL);
        x ^= x >> 30; x *= 0xBF58476D1CE4E5B9UL;
        x ^= x >> 27; x *= 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (x >> 11) * (1.0 / (1UL << 53));
    }
}
