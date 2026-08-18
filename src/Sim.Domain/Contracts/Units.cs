namespace Sim.Domain.Contracts;

/// <summary>
/// Power in kilowatts. Sign convention (ADR-002): consumption is positive,
/// generation is negative. Conversion to energy only via an explicit duration.
/// </summary>
public readonly record struct Kilowatts(double Value) : IComparable<Kilowatts>
{
    public static readonly Kilowatts Zero = new(0);

    public static Kilowatts operator +(Kilowatts a, Kilowatts b) => new(a.Value + b.Value);
    public static Kilowatts operator -(Kilowatts a, Kilowatts b) => new(a.Value - b.Value);
    public static Kilowatts operator -(Kilowatts a) => new(-a.Value);
    public static Kilowatts operator *(Kilowatts a, double factor) => new(a.Value * factor);

    public KilowattHours For(TimeSpan duration) => new(Value * duration.TotalHours);
    public int CompareTo(Kilowatts other) => Value.CompareTo(other.Value);
    public override string ToString() => $"{Value:F3} kW";
}

/// <summary>Energy in kilowatt-hours. Same sign convention as <see cref="Kilowatts"/>.</summary>
public readonly record struct KilowattHours(double Value) : IComparable<KilowattHours>
{
    public static readonly KilowattHours Zero = new(0);

    public static KilowattHours operator +(KilowattHours a, KilowattHours b) => new(a.Value + b.Value);
    public static KilowattHours operator -(KilowattHours a, KilowattHours b) => new(a.Value - b.Value);

    public Kilowatts Over(TimeSpan duration) => new(Value / duration.TotalHours);
    public int CompareTo(KilowattHours other) => Value.CompareTo(other.Value);
    public override string ToString() => $"{Value:F3} kWh";
}
