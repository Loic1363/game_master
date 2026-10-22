using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Commands;

public sealed record AddComponent(string DefinitionId, GridPoint Position, Orientation Orientation)
    : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!catalog.TryGetById(DefinitionId, out var definition))
        {
            return EditFailures.UnknownDefinition(DefinitionId);
        }

        var designator = Designators.Allocate(schematic, definition.DesignatorPrefix);

        var component = new ComponentInstance(
            schematic.NextComponentId,
            definition.Id,
            designator,
            Position,
            Orientation,
            DefaultParameters(definition));

        return CommandResult.Success(schematic.WithComponent(component), $"Add {designator}");
    }

    private static ImmutableSortedDictionary<string, Quantity> DefaultParameters(
        ComponentDefinition definition) =>
        definition.Parameters.Where(parameter => parameter.Default is not null).Aggregate(
            ImmutableSortedDictionary.Create<string, Quantity>(StringComparer.Ordinal),
            (parameters, parameter) => parameters.Add(parameter.Name, parameter.Default!.Value));
}
