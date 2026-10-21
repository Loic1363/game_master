using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record MoveComponent(ComponentId Id, GridPoint Position) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!schematic.TryGetComponent(Id, out var component))
        {
            return EditFailures.UnknownComponent(Id);
        }

        var moved = component with { Position = Position };

        return CommandResult.Success(
            schematic.WithUpdatedComponent(moved), $"Move {component.Designator}");
    }
}
