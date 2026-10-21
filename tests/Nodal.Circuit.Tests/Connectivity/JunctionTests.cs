using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Connectivity;

public class JunctionTests
{
    [Fact]
    public void ALoneWireEndHasNoJunction()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 6, 0);

        Assert.Empty(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }

    [Fact]
    public void TwoWiresMeetingAtACornerAreNotAJunction()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 6, 0)
            .Wire(6, 0, 6, 6);

        Assert.Empty(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }

    [Fact]
    public void APinMeetingOneWireEndIsNotAJunction()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Wire(2, 0, 8, 0);

        Assert.Empty(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }

    [Fact]
    public void TwoPinsTouchingAreNotAJunction()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 4, 0);

        Assert.Empty(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }

    [Fact]
    public void ATJunctionIsAJunction()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 10, 0)
            .Wire(5, 0, 5, 5);

        var junction = Assert.Single(
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);

        Assert.Equal(new GridPoint(5, 0), junction);
    }

    [Fact]
    public void AnXCrossingIsNotAJunction()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 10, 0)
            .Wire(5, -5, 5, 5);

        Assert.Empty(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }

    [Fact]
    public void ThreeWireEndsMeetingAreAJunction()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 5, 0)
            .Wire(5, 0, 10, 0)
            .Wire(5, 0, 5, 5);

        var junction = Assert.Single(
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);

        Assert.Equal(new GridPoint(5, 0), junction);
    }

    [Fact]
    public void FourWireEndsMeetingAreAJunction()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 5, 0)
            .Wire(5, 0, 10, 0)
            .Wire(5, 0, 5, 5)
            .Wire(5, -5, 5, 0);

        Assert.Equal(
            [new GridPoint(5, 0)],
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }

    [Fact]
    public void APinWithTwoWireEndsIsAJunction()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Wire(2, 0, 8, 0)
            .Wire(2, 0, 2, 6);

        var junction = Assert.Single(
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);

        Assert.Equal(new GridPoint(2, 0), junction);
    }

    [Fact]
    public void APinOnAWireInteriorIsAJunction()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 3, 0)
            .Wire(5, -5, 5, 5);

        var junction = Assert.Single(
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);

        Assert.Equal(new GridPoint(5, 0), junction);
    }

    [Fact]
    public void ThreePinsMeetingAreAJunction()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 2, 2, new Orientation(Rotation.R90, false))
            .Add("resistor", 4, 0, new Orientation(Rotation.R0, true));

        var junction = Assert.Single(
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);

        Assert.Equal(new GridPoint(2, 0), junction);
    }

    [Fact]
    public void JunctionsAreSortedInReadingOrder()
    {
        var schematic = Schematic.Empty
            .Wire(0, 10, 20, 10)
            .Wire(5, 10, 5, 20)
            .Wire(15, 10, 15, 20)
            .Wire(0, 0, 20, 0)
            .Wire(10, 0, 10, -5);

        Assert.Equal(
            [new GridPoint(10, 0), new GridPoint(5, 10), new GridPoint(15, 10)],
            ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Junctions);
    }
}
