namespace Sim.SharedKernel;

/// <summary>
/// Pure hash-based noise (SplitMix64 finalizer): the same (seed, stream, point)
/// always yields the same value in [0,1). Stateless by design — reproducibility
/// does not depend on call order, so adding an asset never shifts another
/// asset's random sequence.
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

    /// <summary>FNV-1a over a stable identity string, used to derive a per-entity noise stream.</summary>
    public static ulong StreamOf(string identity)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in identity) { hash ^= c; hash *= 1099511628211UL; }
        return hash;
    }
}
