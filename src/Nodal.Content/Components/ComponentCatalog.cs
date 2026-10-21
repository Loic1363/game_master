using System.Diagnostics.CodeAnalysis;

namespace Nodal.Content.Components;

public sealed class ComponentCatalog
{
    private readonly Dictionary<string, ComponentDefinition> _definitionById;

    public ComponentCatalog(IEnumerable<ComponentDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        Definitions = definitions
            .OrderBy(definition => definition.Id, StringComparer.Ordinal)
            .ToArray();

        _definitionById = new Dictionary<string, ComponentDefinition>(StringComparer.Ordinal);

        foreach (var definition in Definitions)
        {
            if (!_definitionById.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException($"The component id '{definition.Id}' appears twice.", nameof(definitions));
            }
        }
    }

    public static ComponentCatalog Empty { get; } = new([]);

    public IReadOnlyList<ComponentDefinition> Definitions { get; }

    public bool TryGetById(string id, [NotNullWhen(true)] out ComponentDefinition? definition) =>
        _definitionById.TryGetValue(id, out definition);
}
