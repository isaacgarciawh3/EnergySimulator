using Sim.SharedKernel;

namespace Sim.Accounting.Domain;

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
