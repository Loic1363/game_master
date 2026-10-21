using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record SymbolPolyline(ImmutableArray<GridVector> Points, bool Closed) : SymbolPrimitive
{
    public bool Equals(SymbolPolyline? other) =>
        other is not null && Closed == other.Closed && StructuralEquality.ItemsEqual(Points, other.Points);

    public override int GetHashCode() =>
        HashCode.Combine(Closed, StructuralEquality.ItemsHash(Points));
}
