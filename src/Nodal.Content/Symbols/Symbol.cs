using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Content.Symbols;

public sealed record Symbol(
    string ComponentId,
    SymbolStyle Style,
    ImmutableArray<SymbolPrimitive> Primitives)
{
    public bool Equals(Symbol? other) =>
        other is not null
        && string.Equals(ComponentId, other.ComponentId, StringComparison.Ordinal)
        && Style == other.Style
        && StructuralEquality.ItemsEqual(Primitives, other.Primitives);

    public override int GetHashCode() => HashCode.Combine(
        ComponentId, Style, StructuralEquality.ItemsHash(Primitives));
}
