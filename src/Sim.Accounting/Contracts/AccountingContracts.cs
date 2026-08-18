using Sim.SharedKernel;

namespace Sim.Accounting.Contracts;

/// <summary>
/// How Accounting classifies a meter. Deliberately NOT the Energy context's
/// AssetType: accounting cares that something consumes or generates, not that
/// it is a heat pump. The Application layer maps between the two.
/// </summary>
public enum MeterKind { Consumer, Generator, Storage }

/// <summary>One posting into the ledger — the Accounting context's own input language.</summary>
public sealed record EnergyEntry(
    string MeterId,
    string OwnerId,
    string Category,
    MeterKind Kind,
    DateTimeOffset Instant,
    Kilowatts Power,
    KilowattHours Energy);

/// <summary>Settlement of one interval against the grid. Import and export are mutually exclusive.</summary>
public sealed record GridSettlement(
    DateTimeOffset Instant,
    Kilowatts NetPower,
    Kilowatts Import,
    Kilowatts Export,
    KilowattHours ImportedEnergy,
    KilowattHours ExportedEnergy,
    Kilowatts Consumption,
    Kilowatts Generation);
