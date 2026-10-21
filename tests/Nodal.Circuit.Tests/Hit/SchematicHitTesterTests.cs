using Nodal.Circuit.Hit;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Tests.Connectivity;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Hit;

public class SchematicHitTesterTests
{
    [Fact]
    public void PinWithinToleranceIsHit()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var hit = Nearest(schematic, new GridVector(-1.9, 0.05), 0.4);

        Assert.NotNull(hit);
        Assert.Equal(new PinRef(ComponentId.First, "1"), hit.Pin);
        Assert.Null(hit.Wire);
    }

    [Fact]
    public void PinOutsideToleranceIsNotHit()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        Assert.Null(Nearest(schematic, new GridVector(-1, 0), 0.3));
    }

    [Fact]
    public void NearestPinWins()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var hit = Nearest(schematic, new GridVector(1.2, 0), 4);

        Assert.NotNull(hit);
        Assert.Equal(new PinRef(ComponentId.First, "2"), hit.Pin);
        Assert.Equal(0.8, hit.Distance, 9);
    }

    [Fact]
    public void WireWithinToleranceIsHit()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 10, 0);

        var hit = Nearest(schematic, new GridVector(5, 0.2), 0.5);

        Assert.NotNull(hit);
        Assert.Equal(WireId.First, hit.Wire);
        Assert.Null(hit.Pin);
        Assert.Equal(0.2, hit.Distance, 9);
    }

    [Fact]
    public void NearestWireWins()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 10, 0)
            .Wire(0, 2, 10, 2);

        var hit = Nearest(schematic, new GridVector(5, 1.4), 2);

        Assert.NotNull(hit);
        Assert.Equal(new WireId(2), hit.Wire);
        Assert.Equal(0.6, hit.Distance, 9);
    }

    [Fact]
    public void PinTakesPrecedenceOverANearbyWire()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Wire(-2, 1, 5, 1);

        var hit = Nearest(schematic, new GridVector(2, 0.9), 1);

        Assert.NotNull(hit);
        Assert.Equal(new PinRef(ComponentId.First, "2"), hit.Pin);
        Assert.Null(hit.Wire);
    }

    [Fact]
    public void PointBeyondAWireEndIsMeasuredFromThatEnd()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 4, 0);

        Assert.Null(Nearest(schematic, new GridVector(6, 0), 1));

        var hit = Nearest(schematic, new GridVector(4.5, 0), 1);

        Assert.NotNull(hit);
        Assert.Equal(WireId.First, hit.Wire);
        Assert.Equal(0.5, hit.Distance, 9);
    }

    [Fact]
    public void EmptySpaceHitsNothing()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Wire(0, 4, 8, 4);

        Assert.Null(Nearest(schematic, new GridVector(20, 20), 0.5));
    }

    [Fact]
    public void EmptySchematicHitsNothing()
    {
        Assert.Null(Nearest(Schematic.Empty, GridVector.Zero, 1));
    }

    [Fact]
    public void ComponentWithAnUnknownDefinitionIsSkipped()
    {
        var schematic = Schematic.Empty.Add("widget", 0, 0, Circuits.WidgetCatalog());

        Assert.Null(SchematicHitTester.Nearest(
            schematic, BaseCatalog.Catalog, new GridVector(-2, 0), 0.5));
    }

    [Fact]
    public void NegativeToleranceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Nearest(Schematic.Empty, GridVector.Zero, -1));
    }

    private static SchematicHit? Nearest(Schematic schematic, GridVector point, double tolerance) =>
        SchematicHitTester.Nearest(schematic, BaseCatalog.Catalog, point, tolerance);
}
