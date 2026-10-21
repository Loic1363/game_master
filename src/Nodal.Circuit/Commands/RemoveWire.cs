using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record RemoveWire(WireId Id) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!schematic.TryGetWire(Id, out _))
        {
            return EditFailures.UnknownWire(Id);
        }

        return CommandResult.Success(schematic.WithoutWire(Id), "Remove wire");
    }
}
