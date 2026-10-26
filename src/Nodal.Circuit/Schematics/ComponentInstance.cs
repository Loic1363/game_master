using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Schematics;

public sealed record ComponentInstance(
    ComponentId Id,
    string DefinitionId,
    string Designator,
    GridPoint Position,
    Orientation Orientation,
    ImmutableSortedDictionary<string, ParameterValue> Parameters)
{
    public bool Equals(ComponentInstance? other) =>
        other is not null
        && Id == other.Id
        && string.Equals(DefinitionId, other.DefinitionId, StringComparison.Ordinal)
        && string.Equals(Designator, other.Designator, StringComparison.Ordinal)
        && Position == other.Position
        && Orientation == other.Orientation
        && StructuralEquality.EntriesEqual(Parameters, other.Parameters);

    public override int GetHashCode() => HashCode.Combine(
        Id,
        DefinitionId,
        Designator,
        Position,
        Orientation,
        StructuralEquality.EntriesHash(Parameters));
}
