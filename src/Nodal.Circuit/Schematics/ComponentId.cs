using System.Globalization;

namespace Nodal.Circuit.Schematics;

public readonly record struct ComponentId(int Value) : IComparable<ComponentId>
{
    public static ComponentId First { get; } = new(1);

    public ComponentId Next() => new(Value + 1);

    public int CompareTo(ComponentId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
