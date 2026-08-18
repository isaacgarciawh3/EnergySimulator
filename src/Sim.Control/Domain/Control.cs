using Sim.SharedKernel;

namespace Sim.Control.Domain;

/// <summary>What the controller is allowed to see when it decides. Nothing about houses, weather or assets.</summary>
public sealed record GridState(
    Kilowatts NetLoadBeforeStorage,
    double StateOfChargeKwh,
    double CapacityKwh,
    double MaxPowerKw);

/// <summary>A command, not a measurement. Positive charges the battery, negative discharges it.</summary>
public sealed record StorageSetpoint(Kilowatts Power)
{
    public static readonly StorageSetpoint Idle = new(Kilowatts.Zero);
}

public interface IStorageControlStrategy
{
    string Name { get; }
    StorageSetpoint Decide(GridState state, TimeSpan duration);
}
