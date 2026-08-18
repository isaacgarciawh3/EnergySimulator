using Sim.SharedKernel;

namespace Sim.Energy.Contracts;

/// <summary>
/// The Energy context's OWN view of the environment. Deliberately narrower than
/// the Simulation context's TickEnvironment: physics only cares about
/// temperature and irradiance. This context does not know what a "season" or a
/// "cloud" is — the Application layer translates (anti-corruption layer).
/// </summary>
public sealed record EnvironmentInfluence(double TemperatureC, double IrradianceFactor);

/// <summary>Everything an asset may consult when measuring one interval.</summary>
public sealed record MeasurementContext(
    long TickIndex,
    DateTimeOffset Instant,
    TimeSpan Duration,
    EnvironmentInfluence Environment,
    ulong Seed);

public enum AssetType { BaseLoad, HeatPump, Pv, HomeEvCharger, PublicEvCharger }

/// <summary>
/// Published language of the Energy context (A-001): every asset behaves as a
/// meter emitting a signed power measurement per interval. Consumers see
/// readings, never asset internals.
/// </summary>
public sealed record MeterReading(
    string MeterId,
    string OwnerId,
    AssetType Type,
    DateTimeOffset Instant,
    Kilowatts Power,
    KilowattHours Energy);
