using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public interface IEditCommand
{
    CommandResult Apply(Schematic schematic, ComponentCatalog catalog);
}
