using System.Collections.Immutable;
using Nodal.Content.Diagnostics;
using Nodal.Foundation.Collections;

namespace Nodal.Content.Symbols;

public sealed record SymbolLibraryLoadResult(
    SymbolLibrary Library, ImmutableArray<ContentDiagnostic> Diagnostics)
{
    public bool Equals(SymbolLibraryLoadResult? other) =>
        other is not null
        && Library == other.Library
        && StructuralEquality.ItemsEqual(Diagnostics, other.Diagnostics);

    public override int GetHashCode() =>
        HashCode.Combine(Library, StructuralEquality.ItemsHash(Diagnostics));
}
