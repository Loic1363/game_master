using Nodal.Circuit.Geometry;
using Nodal.Circuit.Symbols;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Symbols;

public class LabelPlacementTests
{
    private const double Tolerance = 1e-9;

    public static TheoryData<Rotation, bool, double, double, double, double, LabelAlignment> Placements
    {
        get
        {
            var data = new TheoryData<Rotation, bool, double, double, double, double, LabelAlignment>
            {
                { Rotation.R0, false, 1, -0.7, 1, 1.7, LabelAlignment.Centered },
                { Rotation.R180, false, -1, -1.7, -1, 0.7, LabelAlignment.Centered },
                { Rotation.R90, false, 0.4, 0.7, 0.4, 1.3, LabelAlignment.Left },
                { Rotation.R270, false, 1.4, -1.3, 1.4, -0.7, LabelAlignment.Left },
                { Rotation.R0, true, -1, -0.7, -1, 1.7, LabelAlignment.Centered },
                { Rotation.R180, true, 1, -1.7, 1, 0.7, LabelAlignment.Centered },
                { Rotation.R90, true, 0.4, -1.3, 0.4, -0.7, LabelAlignment.Left },
                { Rotation.R270, true, 1.4, 0.7, 1.4, 1.3, LabelAlignment.Left },
            };

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Placements))]
    public void LabelsSitAroundTheTransformedBoundingBox(
        Rotation rotation,
        bool mirrored,
        double designatorX,
        double designatorY,
        double valueX,
        double valueY,
        LabelAlignment alignment)
    {
        var layout = LabelPlacement.Of(Widget(), new Orientation(rotation, mirrored));

        AssertAnchor(designatorX, designatorY, alignment, layout.Designator);
        AssertAnchor(valueX, valueY, alignment, layout.Value);
    }

    [Fact]
    public void TheGapAndTheLineHeightAreTheDocumentedOnes()
    {
        Assert.Equal(0.4, LabelPlacement.Gap, Tolerance);
        Assert.Equal(0.6, LabelPlacement.LineHeight, Tolerance);
    }

    [Fact]
    public void UprightOrientationsCentreTheDesignatorAboveAndTheValueBelow()
    {
        var layout = LabelPlacement.Of(Resistor(), Orientation.Default);

        AssertAnchor(0, -1.1, LabelAlignment.Centered, layout.Designator);
        AssertAnchor(0, 1.1, LabelAlignment.Centered, layout.Value);
    }

    [Fact]
    public void VerticalOrientationsStackBothLabelsToTheRight()
    {
        var layout = LabelPlacement.Of(Resistor(), new Orientation(Rotation.R90, false));

        AssertAnchor(0.8, -0.3, LabelAlignment.Left, layout.Designator);
        AssertAnchor(0.8, 0.3, LabelAlignment.Left, layout.Value);
    }

    [Fact]
    public void AnchorsAreTheDesignatorAndTheValueInThatOrder()
    {
        var layout = LabelPlacement.Of(Resistor(), Orientation.Default);

        Assert.Equal(
            [layout.Designator.Position, layout.Value.Position],
            LabelPlacement.Anchors(Resistor(), Orientation.Default));
    }

    [Fact]
    public void CornersWithLabelsCoverTheDrawingAndBothAnchors()
    {
        var corners = SymbolExtent.CornersWithLabels(Resistor(), Orientation.Default).ToArray();
        var box = GridBox.Around(corners);

        Assert.Equal(-2, box.Minimum.X, Tolerance);
        Assert.Equal(2, box.Maximum.X, Tolerance);
        Assert.Equal(-1.1, box.Minimum.Y, Tolerance);
        Assert.Equal(1.1, box.Maximum.Y, Tolerance);
    }

    private static void AssertAnchor(
        double x, double y, LabelAlignment alignment, LabelAnchor anchor)
    {
        Assert.Equal(x, anchor.Position.X, Tolerance);
        Assert.Equal(y, anchor.Position.Y, Tolerance);
        Assert.Equal(alignment, anchor.Alignment);
    }

    private static Symbol Widget() => new(
        "widget", SymbolStyle.Iec, [new SymbolRect(0, 0, 2, 1)]);

    private static Symbol Resistor() => new(
        "resistor",
        SymbolStyle.Iec,
        [
            new SymbolLine(new GridVector(-2, 0), new GridVector(-1, 0)),
            new SymbolRect(-1, -0.4, 2, 0.8),
            new SymbolLine(new GridVector(1, 0), new GridVector(2, 0)),
        ]);
}
