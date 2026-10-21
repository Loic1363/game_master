using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Netlists;

public sealed record NetlistElement(
    ComponentId Id,
    string Designator,
    string DefinitionId,
    ImmutableSortedDictionary<string, Quantity> Parameters,
    ImmutableArray<PinConnection> Pins)
{
    public bool Equals(NetlistElement? other) =>
        other is not null
        && Id == other.Id
        && string.Equals(Designator, other.Designator, StringComparison.Ordinal)
        && string.Equals(DefinitionId, other.DefinitionId, StringComparison.Ordinal)
        && StructuralEquality.EntriesEqual(Parameters, other.Parameters)
        && StructuralEquality.ItemsEqual(Pins, other.Pins);

    public override int GetHashCode() => HashCode.Combine(
        Id,
        Designator,
        DefinitionId,
        StructuralEquality.EntriesHash(Parameters),
        StructuralEquality.ItemsHash(Pins));
}
