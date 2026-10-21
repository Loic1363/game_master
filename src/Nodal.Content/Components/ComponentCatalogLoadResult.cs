using System.Collections.Immutable;
using Nodal.Content.Diagnostics;
using Nodal.Foundation.Collections;

namespace Nodal.Content.Components;

public sealed record ComponentCatalogLoadResult(
    ComponentCatalog Catalog, ImmutableArray<ContentDiagnostic> Diagnostics)
{
    public bool Equals(ComponentCatalogLoadResult? other) =>
        other is not null
        && Catalog == other.Catalog
        && StructuralEquality.ItemsEqual(Diagnostics, other.Diagnostics);

    public override int GetHashCode() =>
        HashCode.Combine(Catalog, StructuralEquality.ItemsHash(Diagnostics));
}
