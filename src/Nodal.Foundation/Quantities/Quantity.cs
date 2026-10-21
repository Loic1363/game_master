namespace Nodal.Foundation.Quantities;

public readonly struct Quantity : IEquatable<Quantity>
{
    public Quantity(double value, Unit unit)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A quantity value must be finite.");
        }

        Value = value;
        Unit = unit;
    }

    public double Value { get; }

    public Unit Unit { get; }

    public static bool TryParse(string text, Unit expectedUnit, out Quantity result, out QuantityParseError error) =>
        QuantityParser.TryParse(text, expectedUnit, out result, out error);

    public bool Equals(Quantity other) => Value.Equals(other.Value) && Unit == other.Unit;

    public override bool Equals(object? obj) => obj is Quantity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Unit);

    public static bool operator ==(Quantity left, Quantity right) => left.Equals(right);

    public static bool operator !=(Quantity left, Quantity right) => !left.Equals(right);

    public override string ToString() => QuantityFormatter.Format(Value, Unit);
}
