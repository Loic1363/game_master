using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record AddWire(GridPoint Start, GridPoint End) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (Start == End)
        {
            return CommandResult.Failure(
                EditErrorCode.ZeroLengthWire, $"A wire cannot have zero length at {Start}.");
        }

        if (Start.X != End.X && Start.Y != End.Y)
        {
            return CommandResult.Failure(
                EditErrorCode.DiagonalWire, $"A wire from {Start} to {End} would be diagonal.");
        }

        foreach (var existing in schematic.Wires.Values)
        {
            if (existing.HasEndpoints(Start, End))
            {
                return CommandResult.Failure(
                    EditErrorCode.DuplicateWire, $"A wire already joins {Start} and {End}.");
            }
        }

        var wire = new Wire(schematic.NextWireId, Start, End);

        return CommandResult.Success(schematic.WithWire(wire), "Add wire");
    }
}
