using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record RotateComponent(ComponentId Id) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!schematic.TryGetComponent(Id, out var component))
        {
            return EditFailures.UnknownComponent(Id);
        }

        var rotated = component with { Orientation = component.Orientation.RotatedClockwise() };

        return CommandResult.Success(
            schematic.WithUpdatedComponent(rotated), $"Rotate {component.Designator}");
    }
}
