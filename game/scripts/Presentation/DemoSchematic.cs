using Godot;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Orientation = Nodal.Circuit.Geometry.Orientation;

namespace Nodal.Game.Presentation;

public static class DemoSchematic
{
    public static EditHistory Build(ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var history = new EditHistory(Schematic.Empty, catalog);

        Run(history, new AddComponent("voltage-source", new GridPoint(0, 0), Orientation.Default));
        Run(history, new AddComponent("resistor", new GridPoint(6, -4), Orientation.Default));
        Run(history, new AddComponent("capacitor", new GridPoint(8, 0), new Orientation(Rotation.R90, false)));
        Run(history, new AddComponent("ground", new GridPoint(0, 4), Orientation.Default));
        Run(history, new AddComponent("ground", new GridPoint(8, 4), Orientation.Default));

        Run(history, new AddWire(new GridPoint(0, -2), new GridPoint(0, -4)));
        Run(history, new AddWire(new GridPoint(0, -4), new GridPoint(4, -4)));
        Run(history, new AddWire(new GridPoint(8, -4), new GridPoint(8, -2)));
        Run(history, new AddWire(new GridPoint(0, 2), new GridPoint(8, 2)));
        Run(history, new AddWire(new GridPoint(0, 2), new GridPoint(0, 4)));
        Run(history, new AddWire(new GridPoint(8, 2), new GridPoint(8, 4)));

        return history;
    }

    private static void Run(EditHistory history, IEditCommand command)
    {
        var result = history.Execute(command);

        if (!result.Succeeded)
        {
            GD.PrintErr($"The demo schematic could not apply {command}: {result.Error}");
        }
    }
}
