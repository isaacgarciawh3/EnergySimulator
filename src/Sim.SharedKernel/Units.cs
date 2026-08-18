namespace Sim.SharedKernel;

/// <summary>
/// Power in kilowatts. Sign convention (ADR-002): consumption positive,
/// generation negative. Converts to energy only via an explicit duration —
/// mixing kW and kWh is the classic energy-simulation bug, so the type system
/// forbids it.
/// </summary>
public readonly record struct Kilowatts(double Value) : IComparable<Kilowatts>
{
    public static readonly Kilowatts Zero = new(0);

    public static Kilowatts operator +(Kilowatts a, Kilowatts b) => new(a.Value + b.Value);
    public static Kilowatts operator -(Kilowatts a, Kilowatts b) => new(a.Value - b.Value);
    public static Kilowatts operator -(Kilowatts a) => new(-a.Value);

    public KilowattHours Over(TimeSpan duration) => new(Value * duration.TotalHours);
    public int CompareTo(Kilowatts other) => Value.CompareTo(other.Value);
    public override string ToString() => $"{Value:F3} kW";
}

/// <summary>Energy in kilowatt-hours. Same sign convention as <see cref="Kilowatts"/>.</summary>
public readonly record struct KilowattHours(double Value) : IComparable<KilowattHours>
{
    public static readonly KilowattHours Zero = new(0);

    public static KilowattHours operator +(KilowattHours a, KilowattHours b) => new(a.Value + b.Value);
    public static KilowattHours operator -(KilowattHours a, KilowattHours b) => new(a.Value - b.Value);

    public int CompareTo(KilowattHours other) => Value.CompareTo(other.Value);
    public override string ToString() => $"{Value:F3} kWh";
}
