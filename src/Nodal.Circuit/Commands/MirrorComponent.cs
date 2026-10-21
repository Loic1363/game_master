using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record MirrorComponent(ComponentId Id) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!schematic.TryGetComponent(Id, out var component))
        {
            return EditFailures.UnknownComponent(Id);
        }

        var mirrored = component with { Orientation = component.Orientation.Flipped() };

        return CommandResult.Success(
            schematic.WithUpdatedComponent(mirrored), $"Mirror {component.Designator}");
    }
}
