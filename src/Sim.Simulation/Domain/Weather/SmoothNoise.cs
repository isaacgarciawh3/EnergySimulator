namespace Sim.Simulation.Domain.Weather;

/// <summary>
/// Value noise: a continuous random-looking signal built by drawing one hash
/// value per fixed-length block of time and interpolating between them.
///
/// Raw hash noise per tick would make the weather jitter wildly from one
/// interval to the next. Real weather is correlated over hours, so we draw a
/// value every <c>correlationPeriod</c> and blend across it. Three separate
/// rules are involved and each is a named, separately testable function:
/// which block an instant falls in, how far through it we are, and how to blend.
/// </summary>
public static class SmoothNoise
{
    /// <summary>Locates an instant: which block it falls in, and how far through that block it is (0 to 1).</summary>
    public static (long Block, double Fraction) Locate(DateTimeOffset instant, TimeSpan correlationPeriod)
    {
        if (correlationPeriod <= TimeSpan.Zero)
            throw new Sim.Simulation.Domain.SimulationInvariantViolation("SmoothNoise correlation period must be positive.");

        var periodSeconds = (long)correlationPeriod.TotalSeconds;
        var block = Math.DivRem(instant.ToUnixTimeSeconds(), periodSeconds, out var remainder);

        // Instants before the epoch produce a negative remainder; shift into the
        // previous block so that Fraction is always within [0, 1).
        if (remainder < 0) { block -= 1; remainder += periodSeconds; }

        return (block, (double)remainder / periodSeconds);
    }

    /// <summary>Linear blend. Separated out because "how we interpolate" is a decision, not an expression.</summary>
    public static double Blend(double from, double to, double fraction) => from + (to - from) * fraction;

    /// <summary>
    /// A smooth value in [0, 1) that is continuous across block boundaries and
    /// fully determined by (seed, stream, instant).
    /// </summary>
    public static double At(ulong seed, ulong stream, DateTimeOffset instant, TimeSpan correlationPeriod)
    {
        var (block, fraction) = Locate(instant, correlationPeriod);
        var start = DeterministicNoise.Sample(seed, stream, block);
        var end = DeterministicNoise.Sample(seed, stream, block + 1);
        return Blend(start, end, fraction);
    }
}
