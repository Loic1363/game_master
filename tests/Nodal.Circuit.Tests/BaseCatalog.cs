using Nodal.Content.Components;
using Nodal.Content.Sources;

namespace Nodal.Circuit.Tests;

internal static class BaseCatalog
{
    public static ComponentCatalog Catalog { get; } = Load();

    private static ComponentCatalog Load()
    {
        var result = ComponentCatalogLoader.Load(
            new DirectoryContentSource(RepositoryPaths.ComponentsDirectory));

        return result.Diagnostics.Count == 0
            ? result.Catalog
            : throw new InvalidOperationException(
                $"The base catalog reported {result.Diagnostics.Count} diagnostics.");
    }
}
