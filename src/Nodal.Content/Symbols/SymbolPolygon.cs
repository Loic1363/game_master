using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record SymbolPolygon(ImmutableArray<GridVector> Points, bool Filled) : SymbolPrimitive
{
    public bool Equals(SymbolPolygon? other) =>
        other is not null && Filled == other.Filled && StructuralEquality.ItemsEqual(Points, other.Points);

    public override int GetHashCode() =>
        HashCode.Combine(Filled, StructuralEquality.ItemsHash(Points));
}
