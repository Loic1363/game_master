using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record Symbol(
    string ComponentId,
    SymbolStyle Style,
    ImmutableArray<SymbolPrimitive> Primitives,
    GridVector DesignatorAnchor,
    GridVector? ValueAnchor)
{
    public bool Equals(Symbol? other) =>
        other is not null
        && string.Equals(ComponentId, other.ComponentId, StringComparison.Ordinal)
        && Style == other.Style
        && DesignatorAnchor == other.DesignatorAnchor
        && ValueAnchor == other.ValueAnchor
        && StructuralEquality.ItemsEqual(Primitives, other.Primitives);

    public override int GetHashCode() => HashCode.Combine(
        ComponentId, Style, DesignatorAnchor, ValueAnchor, StructuralEquality.ItemsHash(Primitives));
}
