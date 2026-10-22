using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Tests.Connectivity;

internal static class Circuits
{
    public static Schematic Add(
        this Schematic schematic, string definitionId, int x, int y) =>
        schematic.Add(definitionId, x, y, Orientation.Default);

    public static Schematic Add(
        this Schematic schematic, string definitionId, int x, int y, Orientation orientation) =>
        new AddComponent(definitionId, new GridPoint(x, y), orientation).Succeed(schematic);

    public static Schematic Add(
        this Schematic schematic,
        string definitionId,
        int x,
        int y,
        ComponentCatalog catalog) =>
        new AddComponent(definitionId, new GridPoint(x, y), Orientation.Default)
            .Succeed(schematic, catalog);

    public static Schematic Wire(this Schematic schematic, int startX, int startY, int endX, int endY) =>
        new AddWire(new GridPoint(startX, startY), new GridPoint(endX, endY)).Succeed(schematic);

    public static ComponentCatalog GlobalNetCatalog() => new([
        Reference("gnd-symbol", "G", "0"),
        Reference("vcc-symbol", "VCC", "VCC"),
    ]);

    public static ComponentCatalog WidgetCatalog() => new([
        new ComponentDefinition(
            "widget",
            "Widget",
            "A widget.",
            ComponentCategory.Passive,
            "W",
            [new PinDefinition("a", -2, 0), new PinDefinition("b", 2, 0)],
            [new ParameterDefinition(
                "gain",
                "Gain",
                ParameterTier.Basic,
                Unit.Dimensionless,
                new Quantity(2, Unit.Dimensionless),
                null,
                null,
                null)]),
    ]);

    private static ComponentDefinition Reference(string id, string prefix, string globalNet) =>
        new(id,
            id,
            "A reference symbol.",
            ComponentCategory.Reference,
            prefix,
            [new PinDefinition("p", 0, 0, globalNet)],
            []);
}
