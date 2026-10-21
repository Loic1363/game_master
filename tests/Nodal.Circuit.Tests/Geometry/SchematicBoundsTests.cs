using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Tests.Connectivity;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Geometry;

public class SchematicBoundsTests
{
    [Fact]
    public void EmptySchematicHasNoBounds()
    {
        Assert.Null(Bounds(Schematic.Empty));
    }

    [Fact]
    public void BoundsSpanPinPositionsAndWireEnds()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 6, -4)
            .Wire(0, 2, 8, 2);

        Assert.Equal(new GridBounds(new GridPoint(0, -4), new GridPoint(8, 2)), Bounds(schematic));
    }

    [Fact]
    public void RotatedComponentContributesItsTransformedPins()
    {
        var schematic = Schematic.Empty.Add("capacitor", 8, 0, new Orientation(Rotation.R90, false));

        Assert.Equal(new GridBounds(new GridPoint(8, -2), new GridPoint(8, 2)), Bounds(schematic));
    }

    [Fact]
    public void ComponentWithAnUnknownDefinitionContributesItsPosition()
    {
        var schematic = Schematic.Empty.Add("widget", 5, 5, Circuits.WidgetCatalog());

        Assert.Equal(new GridBounds(new GridPoint(5, 5), new GridPoint(5, 5)), Bounds(schematic));
    }

    [Fact]
    public void BoundsCoverTheSymbolDrawingAndItsLabelAnchors()
    {
        var schematic = Schematic.Empty.Add("ground", 0, 4);

        var withoutSymbols = Bounds(schematic);
        var withSymbols = SchematicBounds.Of(schematic, BaseCatalog.Catalog, BaseSymbols.Library);

        Assert.Equal(new GridBounds(new GridPoint(0, 4), new GridPoint(0, 4)), withoutSymbols);
        Assert.Equal(new GridBounds(new GridPoint(-1, 4), new GridPoint(1, 6)), withSymbols);
    }

    [Fact]
    public void GrownBoundsAddTheMarginOnEverySide()
    {
        var bounds = new GridBounds(new GridPoint(0, -4), new GridPoint(8, 2)).Grown(2);

        Assert.Equal(new GridBounds(new GridPoint(-2, -6), new GridPoint(10, 4)), bounds);
        Assert.Equal(12, bounds.Width);
        Assert.Equal(10, bounds.Height);
    }

    [Fact]
    public void CentreSitsHalfwayBetweenTheCorners()
    {
        var bounds = new GridBounds(new GridPoint(0, -4), new GridPoint(3, 2));

        Assert.Equal(new GridVector(1.5, -1), bounds.Center);
    }

    [Fact]
    public void BoundsAroundNoPointIsRejected()
    {
        Assert.Throws<ArgumentException>(() => GridBounds.Around([]));
    }

    private static GridBounds? Bounds(Schematic schematic) =>
        SchematicBounds.Of(schematic, BaseCatalog.Catalog, SymbolLibrary.Empty);
}
