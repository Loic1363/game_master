using Nodal.Content.Components;
using Nodal.Content.Sources;
using Nodal.Content.Symbols;

namespace Nodal.Editor.Tests;

internal static class BaseContent
{
    public static ComponentCatalog Catalog { get; } = LoadCatalog();

    public static SymbolLibrary Symbols { get; } = LoadSymbols();

    private static ComponentCatalog LoadCatalog()
    {
        var result = ComponentCatalogLoader.Load(
            new DirectoryContentSource(RepositoryPaths.ComponentsDirectory));

        return result.Diagnostics.Length == 0
            ? result.Catalog
            : throw new InvalidOperationException(
                $"The base catalog reported {result.Diagnostics.Length} diagnostics.");
    }

    private static SymbolLibrary LoadSymbols()
    {
        var result = SymbolLibraryLoader.Load(
            new DirectoryContentSource(RepositoryPaths.SymbolsDirectory), Catalog);

        return result.Diagnostics.Length == 0
            ? result.Library
            : throw new InvalidOperationException(
                $"The base symbol library reported {result.Diagnostics.Length} diagnostics.");
    }
}
