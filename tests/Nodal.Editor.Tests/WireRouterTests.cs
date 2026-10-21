using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Editor.Tests;

public class WireRouterTests
{
    [Fact]
    public void HorizontalRouteIsOneSegment()
    {
        var segments = Route(Schematic.Empty, 0, 0, 6, 0);

        Assert.Equal([Segment(0, 0, 6, 0)], segments);
    }

    [Fact]
    public void VerticalRouteIsOneSegment()
    {
        var segments = Route(Schematic.Empty, 0, 0, 0, 6);

        Assert.Equal([Segment(0, 0, 0, 6)], segments);
    }

    [Fact]
    public void DiagonalRouteTurnsHorizontallyFirst()
    {
        var segments = Route(Schematic.Empty, 0, 0, 4, 3);

        Assert.Equal([Segment(0, 0, 4, 0), Segment(4, 0, 4, 3)], segments);
    }

    [Fact]
    public void RouteToTheSamePointIsEmpty() =>
        Assert.Empty(Route(Schematic.Empty, 2, 2, 2, 2));

    [Fact]
    public void SegmentAlreadyWiredIsSkipped()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 4, 0);

        Assert.Equal([Segment(4, 0, 4, 3)], Route(schematic, 0, 0, 4, 3));
    }

    [Fact]
    public void SegmentAlreadyWiredInReverseIsSkipped()
    {
        var schematic = Schematic.Empty.Wire(4, 0, 0, 0);

        Assert.Equal([Segment(4, 0, 4, 3)], Route(schematic, 0, 0, 4, 3));
    }

    [Fact]
    public void RouteWhoseSegmentsAllExistIsEmpty()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 4, 0)
            .Wire(4, 0, 4, 3);

        Assert.Empty(Route(schematic, 0, 0, 4, 3));
    }

    [Fact]
    public void AnOverlappingButDifferentWireDoesNotSkipTheSegment()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 8, 0);

        Assert.Equal([Segment(0, 0, 4, 0)], Route(schematic, 0, 0, 4, 0));
    }

    [Fact]
    public void PreviewKeepsTheSegmentsEvenWhenTheyExist()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 4, 0);

        Assert.Empty(Route(schematic, 0, 0, 4, 0));
        Assert.Equal(
            [Segment(0, 0, 4, 0)],
            WireRouter.Preview(new GridPoint(0, 0), new GridPoint(4, 0)));
    }

    [Fact]
    public void PreviewOfAPointIsEmpty() =>
        Assert.Empty(WireRouter.Preview(new GridPoint(1, 1), new GridPoint(1, 1)));

    [Fact]
    public void RoutingWithoutASchematicIsRejected() => Assert.Throws<ArgumentNullException>(
        () => WireRouter.Route(null!, GridPoint.Origin, new GridPoint(1, 0)));

    private static IReadOnlyList<WireSegment> Route(
        Schematic schematic, int startX, int startY, int endX, int endY) =>
        WireRouter.Route(schematic, new GridPoint(startX, startY), new GridPoint(endX, endY));

    private static WireSegment Segment(int startX, int startY, int endX, int endY) =>
        new(new GridPoint(startX, startY), new GridPoint(endX, endY));
}
