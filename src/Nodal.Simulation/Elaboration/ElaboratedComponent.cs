using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;

namespace Nodal.Simulation.Elaboration;

public sealed record ElaboratedComponent(
    ComponentId Id,
    string Designator,
    ComponentKind Kind,
    string PlusNet,
    string MinusNet,
    ImmutableArray<int> Primitives,
    ImmutableArray<int> TerminalPrimitives,
    ElementRatings Ratings)
{
    public bool Equals(ElaboratedComponent? other) =>
        other is not null
        && Id == other.Id
        && string.Equals(Designator, other.Designator, StringComparison.Ordinal)
        && Kind == other.Kind
        && string.Equals(PlusNet, other.PlusNet, StringComparison.Ordinal)
        && string.Equals(MinusNet, other.MinusNet, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Primitives, other.Primitives)
        && StructuralEquality.ItemsEqual(TerminalPrimitives, other.TerminalPrimitives)
        && Ratings == other.Ratings;

    public override int GetHashCode() => HashCode.Combine(
        Id,
        Designator,
        Kind,
        PlusNet,
        MinusNet,
        StructuralEquality.ItemsHash(Primitives),
        StructuralEquality.ItemsHash(TerminalPrimitives),
        Ratings);
}
