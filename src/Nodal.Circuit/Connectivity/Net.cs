using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;

namespace Nodal.Circuit.Connectivity;

public sealed record Net(
    string Name, ImmutableArray<PinRef> Pins, ImmutableArray<WireId> Wires)
{
    public bool Equals(Net? other) =>
        other is not null
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Pins, other.Pins)
        && StructuralEquality.ItemsEqual(Wires, other.Wires);

    public override int GetHashCode() => HashCode.Combine(
        Name, StructuralEquality.ItemsHash(Pins), StructuralEquality.ItemsHash(Wires));
}
