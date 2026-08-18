using Sim.SharedKernel;

namespace Sim.Control.Domain;

/// <summary>
/// What the controller is allowed to see when it decides - one number and the
/// battery's limits, nothing about houses, weather or assets. Born valid or
/// not born: a nonsense state would make the strategy compute nonsense
/// setpoints without complaint.
/// </summary>
public sealed record GridState
{
    public GridState(Kilowatts netLoadBeforeStorage, double stateOfChargeKwh, double capacityKwh, double maxPowerKw)
    {
        Refuse(capacityKwh <= 0, nameof(CapacityKwh), "must be positive");
        Refuse(maxPowerKw <= 0, nameof(MaxPowerKw), "must be positive");
        Refuse(stateOfChargeKwh < 0, nameof(StateOfChargeKwh), "cannot be negative");
        Refuse(stateOfChargeKwh > capacityKwh, nameof(StateOfChargeKwh), "cannot exceed the capacity");

        NetLoadBeforeStorage = netLoadBeforeStorage;
        StateOfChargeKwh = stateOfChargeKwh;
        CapacityKwh = capacityKwh;
        MaxPowerKw = maxPowerKw;
    }

    private static void Refuse(bool violated, string name, string requirement)
    {
        if (violated) throw new ControlInvariantViolation($"GridState.{name} {requirement}.");
    }

    public Kilowatts NetLoadBeforeStorage { get; }
    public double StateOfChargeKwh { get; }
    public double CapacityKwh { get; }
    public double MaxPowerKw { get; }
}

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

/// <summary>Raised when a rule of the Control context would be violated. One type for the whole context: the message names the field and the rule.</summary>
public sealed class ControlInvariantViolation(string message) : InvalidOperationException(message);
