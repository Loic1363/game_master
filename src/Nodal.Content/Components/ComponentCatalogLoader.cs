using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;

namespace Nodal.Content.Components;

public static class ComponentCatalogLoader
{
    public static ComponentCatalogLoadResult Load(IContentSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var diagnostics = new List<ContentDiagnostic>();
        var definitions = new List<ComponentDefinition>();
        var sourceFileById = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in source.ReadAll())
        {
            var definition = ComponentDefinitionReader.Read(file, diagnostics);

            if (definition is null)
            {
                continue;
            }

            if (sourceFileById.TryGetValue(definition.Id, out var owner))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    file.Name,
                    JsonPath.Field(JsonPath.Root, ComponentFields.Id),
                    $"The component id '{definition.Id}' is already defined by {owner}."));
                continue;
            }

            sourceFileById.Add(definition.Id, file.Name);
            definitions.Add(definition);
        }

        return new ComponentCatalogLoadResult(new ComponentCatalog(definitions), diagnostics);
    }
}
