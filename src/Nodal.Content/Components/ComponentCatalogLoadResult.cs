using Nodal.Content.Diagnostics;

namespace Nodal.Content.Components;

public sealed record ComponentCatalogLoadResult(
    ComponentCatalog Catalog, IReadOnlyList<ContentDiagnostic> Diagnostics);
