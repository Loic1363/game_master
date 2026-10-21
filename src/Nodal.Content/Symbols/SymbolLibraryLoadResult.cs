using Nodal.Content.Diagnostics;

namespace Nodal.Content.Symbols;

public sealed record SymbolLibraryLoadResult(
    SymbolLibrary Library, IReadOnlyList<ContentDiagnostic> Diagnostics);
