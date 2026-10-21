using System.Globalization;

namespace Nodal.Circuit.Schematics;

public readonly record struct WireId(int Value) : IComparable<WireId>
{
    public static WireId First { get; } = new(1);

    public WireId Next() => new(Value + 1);

    public int CompareTo(WireId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
