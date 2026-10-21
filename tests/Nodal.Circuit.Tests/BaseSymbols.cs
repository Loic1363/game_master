using Nodal.Content.Sources;
using Nodal.Content.Symbols;

namespace Nodal.Circuit.Tests;

internal static class BaseSymbols
{
    public static SymbolLibrary Library { get; } = Load();

    private static SymbolLibrary Load()
    {
        var result = SymbolLibraryLoader.Load(
            new DirectoryContentSource(RepositoryPaths.SymbolsDirectory), BaseCatalog.Catalog);

        return result.Diagnostics.Length == 0
            ? result.Library
            : throw new InvalidOperationException(
                $"The base symbol library reported {result.Diagnostics.Length} diagnostics.");
    }
}
