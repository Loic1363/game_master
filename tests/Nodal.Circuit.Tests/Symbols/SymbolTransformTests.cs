using Nodal.Circuit.Geometry;
using Nodal.Circuit.Symbols;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Symbols;

public class SymbolTransformTests
{
    [Theory]
    [InlineData(Rotation.R0, false, 2, 1)]
    [InlineData(Rotation.R90, false, -1, 2)]
    [InlineData(Rotation.R180, false, -2, -1)]
    [InlineData(Rotation.R270, false, 1, -2)]
    [InlineData(Rotation.R0, true, -2, 1)]
    [InlineData(Rotation.R90, true, -1, -2)]
    [InlineData(Rotation.R180, true, 2, -1)]
    [InlineData(Rotation.R270, true, 1, 2)]
    public void PointFollowsTheMirrorThenRotateOrder(
        Rotation rotation, bool mirrored, double expectedX, double expectedY)
    {
        var moved = SymbolTransform.Apply(new GridVector(2, 1), new Orientation(rotation, mirrored));

        Assert.Equal(new GridVector(expectedX, expectedY), moved);
    }

    [Fact]
    public void PointTransformMatchesThePinTransform()
    {
        foreach (var orientation in Orientations())
        {
            var pin = orientation.Transform(new GridPoint(2, 1));
            var point = SymbolTransform.Apply(new GridVector(2, 1), orientation);

            Assert.Equal(new GridVector(pin.X, pin.Y), point);
        }
    }

    [Fact]
    public void RotatedLineMovesBothEnds()
    {
        var line = Assert.IsType<SymbolLine>(Apply(
            new SymbolLine(new GridVector(1, 0), new GridVector(2, 0)), Rotation.R90, false));

        Assert.Equal(new GridVector(0, 1), line.From);
        Assert.Equal(new GridVector(0, 2), line.To);
    }

    [Fact]
    public void MirroredLineFlipsX()
    {
        var line = Assert.IsType<SymbolLine>(Apply(
            new SymbolLine(new GridVector(1, 0.5), new GridVector(2, 0.5)), Rotation.R0, true));

        Assert.Equal(new GridVector(-1, 0.5), line.From);
        Assert.Equal(new GridVector(-2, 0.5), line.To);
    }

    [Fact]
    public void RotatedPolylineKeepsItsClosedFlag()
    {
        var polyline = Assert.IsType<SymbolPolyline>(Apply(
            new SymbolPolyline([new GridVector(1, 0), new GridVector(1, 1)], true),
            Rotation.R90,
            false));

        Assert.Equal([new GridVector(0, 1), new GridVector(-1, 1)], polyline.Points);
        Assert.True(polyline.Closed);
    }

    [Fact]
    public void MirroredPolygonKeepsItsFilledFlag()
    {
        var polygon = Assert.IsType<SymbolPolygon>(Apply(
            new SymbolPolygon([new GridVector(1, 0), new GridVector(2, 0), new GridVector(2, 1)], true),
            Rotation.R0,
            true));

        Assert.Equal(
            [new GridVector(-1, 0), new GridVector(-2, 0), new GridVector(-2, 1)], polygon.Points);

        Assert.True(polygon.Filled);
    }

    [Fact]
    public void RotatedRectStaysANormalisedRect()
    {
        var rect = Assert.IsType<SymbolRect>(Apply(
            new SymbolRect(-1, -0.4, 2, 0.8), Rotation.R90, false));

        Assert.Equal(new SymbolRect(-0.4, -1, 0.8, 2), rect);
    }

    [Fact]
    public void MirroredRectStaysANormalisedRect()
    {
        var rect = Assert.IsType<SymbolRect>(Apply(new SymbolRect(0.5, -0.4, 2, 0.8), Rotation.R0, true));

        Assert.Equal(new SymbolRect(-2.5, -0.4, 2, 0.8), rect);
    }

    [Fact]
    public void RotatedCircleKeepsItsRadius()
    {
        var circle = Assert.IsType<SymbolCircle>(Apply(
            new SymbolCircle(new GridVector(0, 1), 0.5, true), Rotation.R90, false));

        Assert.Equal(new SymbolCircle(new GridVector(-1, 0), 0.5, true), circle);
    }

    [Theory]
    [InlineData(Rotation.R0, false, 180, 180)]
    [InlineData(Rotation.R90, false, 270, 180)]
    [InlineData(Rotation.R180, false, 0, 180)]
    [InlineData(Rotation.R270, false, 90, 180)]
    [InlineData(Rotation.R0, true, 0, -180)]
    [InlineData(Rotation.R90, true, 90, -180)]
    public void ArcMirrorReversesTheSweepAndRotationAddsToTheAngles(
        Rotation rotation, bool mirrored, double expectedStart, double expectedSweep)
    {
        var arc = Assert.IsType<SymbolArc>(Apply(
            new SymbolArc(new GridVector(0.9, 0), 0.3, 180, 180), rotation, mirrored));

        Assert.Equal(expectedStart, arc.StartDegrees);
        Assert.Equal(expectedSweep, arc.SweepDegrees);
        Assert.Equal(0.3, arc.Radius);
    }

    [Fact]
    public void MirroredArcCentreFollowsTheMirror()
    {
        var arc = Assert.IsType<SymbolArc>(Apply(
            new SymbolArc(new GridVector(0.9, 0.2), 0.3, 45, 90), Rotation.R0, true));

        Assert.Equal(new GridVector(-0.9, 0.2), arc.Center);
        Assert.Equal(135, arc.StartDegrees);
        Assert.Equal(-90, arc.SweepDegrees);
    }

    [Fact]
    public void TextKeepsItsValueAndOnlyMovesItsAnchor()
    {
        var text = Assert.IsType<SymbolText>(Apply(
            new SymbolText(new GridVector(0.5, -0.5), "+"), Rotation.R90, false));

        Assert.Equal(new GridVector(0.5, 0.5), text.At);
        Assert.Equal("+", text.Value);
    }

    [Fact]
    public void SymbolTransformMovesLabelAnchorsAndEveryPrimitive()
    {
        var symbol = new Symbol(
            "widget",
            SymbolStyle.Iec,
            [new SymbolLine(new GridVector(-2, 0), new GridVector(2, 0))],
            new GridVector(0, -1.2),
            new GridVector(0, 1.2));

        var rotated = SymbolTransform.Apply(symbol, new Orientation(Rotation.R90, false));

        Assert.Equal(new GridVector(1.2, 0), rotated.DesignatorAnchor);
        Assert.Equal(new GridVector(-1.2, 0), rotated.ValueAnchor);
        Assert.Equal(
            new SymbolLine(new GridVector(0, -2), new GridVector(0, 2)),
            Assert.Single(rotated.Primitives));

        Assert.Equal("widget", rotated.ComponentId);
        Assert.Equal(SymbolStyle.Iec, rotated.Style);
    }

    [Fact]
    public void SymbolWithoutAValueAnchorKeepsNone()
    {
        var symbol = new Symbol(
            "widget",
            SymbolStyle.Iec,
            [new SymbolLine(new GridVector(-2, 0), new GridVector(2, 0))],
            new GridVector(0, -1.2),
            null);

        Assert.Null(SymbolTransform.Apply(symbol, new Orientation(Rotation.R180, true)).ValueAnchor);
    }

    [Fact]
    public void DefaultOrientationChangesNothing()
    {
        var symbol = new Symbol(
            "widget",
            SymbolStyle.Iec,
            [
                new SymbolArc(new GridVector(0.9, 0), 0.3, 180, 180),
                new SymbolRect(-1, -0.4, 2, 0.8),
            ],
            new GridVector(0, -1.2),
            new GridVector(0, 1.2));

        Assert.Equal(symbol, SymbolTransform.Apply(symbol, Orientation.Default));
    }

    private static SymbolPrimitive Apply(SymbolPrimitive primitive, Rotation rotation, bool mirrored) =>
        SymbolTransform.Apply(primitive, new Orientation(rotation, mirrored));

    private static IEnumerable<Orientation> Orientations()
    {
        foreach (var rotation in new[] { Rotation.R0, Rotation.R90, Rotation.R180, Rotation.R270 })
        {
            yield return new Orientation(rotation, false);
            yield return new Orientation(rotation, true);
        }
    }
}
