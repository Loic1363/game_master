using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Tests.Persistence;

internal static class GoldenSchematic
{
    public const string FileName = "golden-schematic.nodal";

    public static Schematic Build()
    {
        var schematic = Schematic.Empty;

        schematic = Add(schematic, "voltage-source", new GridPoint(0, 0), Orientation.Default);
        schematic = Add(schematic, "resistor", new GridPoint(6, -4), Orientation.Default);
        schematic = Add(schematic, "capacitor", new GridPoint(8, 0), new Orientation(Rotation.R90, false));
        schematic = Add(schematic, "ground", new GridPoint(0, 4), Orientation.Default);
        schematic = Add(schematic, "ground", new GridPoint(8, 4), Orientation.Default);

        schematic = Connect(schematic, new GridPoint(0, -2), new GridPoint(0, -4));
        schematic = Connect(schematic, new GridPoint(0, -4), new GridPoint(4, -4));
        schematic = Connect(schematic, new GridPoint(8, -4), new GridPoint(8, -2));
        schematic = Connect(schematic, new GridPoint(0, 2), new GridPoint(8, 2));
        schematic = Connect(schematic, new GridPoint(0, 2), new GridPoint(0, 4));
        schematic = Connect(schematic, new GridPoint(8, 2), new GridPoint(8, 4));

        return schematic;
    }

    public static string GoldenText() =>
        File.ReadAllText(Path.Combine(RepositoryPaths.PersistenceDirectory, FileName));

    private static Schematic Add(
        Schematic schematic, string definitionId, GridPoint position, Orientation orientation) =>
        new AddComponent(definitionId, position, orientation).Succeed(schematic);

    private static Schematic Connect(Schematic schematic, GridPoint start, GridPoint end) =>
        new AddWire(start, end).Succeed(schematic);
}
