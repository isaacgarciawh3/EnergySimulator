namespace Sim.SharedKernel;

/// <summary>
/// The stable telemetry contract of the platform: at this instant, this meter
/// was drawing (positive) or delivering (negative) this much power.
///
/// It lives in the shared kernel deliberately. Today a simulation produces
/// these; tomorrow an IoT gateway does. Neither the Energy model nor the
/// Accounting ledger should have to change when the producer changes, so the
/// contract cannot be owned by the producer.
/// </summary>
public sealed record PowerReading(string MeterId, DateTimeOffset Instant, Kilowatts Power);
