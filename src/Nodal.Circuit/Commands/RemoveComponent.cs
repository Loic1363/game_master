using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record RemoveComponent(ComponentId Id) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!schematic.TryGetComponent(Id, out var component))
        {
            return EditFailures.UnknownComponent(Id);
        }

        return CommandResult.Success(
            schematic.WithoutComponent(Id), $"Remove {component.Designator}");
    }
}
