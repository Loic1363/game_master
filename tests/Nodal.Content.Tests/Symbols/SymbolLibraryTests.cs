using System.Collections.Immutable;
using Nodal.Content.Sources;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Content.Tests.Symbols;

public class SymbolLibraryTests
{
    [Fact]
    public void ValidFileLoadsEveryPrimitiveKind()
    {
        var json = SymbolJson.Valid()
            .With(
                "primitives",
                """
                [{ "type": "line", "from": [-2, 0], "to": [2, 0] },
                 { "type": "polyline", "points": [[0, 0], [1, 0], [1, 1]], "closed": true },
                 { "type": "rect", "x": -1, "y": -0.4, "width": 2, "height": 0.8 },
                 { "type": "circle", "center": [0, 0], "radius": 1 },
                 { "type": "circle", "center": [0, 0], "radius": 0.2, "filled": true },
                 { "type": "arc", "center": [0, 0], "radius": 0.3, "startDegrees": 180, "sweepDegrees": 180 },
                 { "type": "polygon", "points": [[0, 0], [1, 0], [0, 1]], "filled": true },
                 { "type": "text", "at": [0.5, -0.5], "value": "+" }]
                """)
            .ToString();

        var result = SymbolLibraryLoader.Load(
            new InMemoryContentSource(new ContentFile("widget.iec.json", json)),
            SymbolCatalogs.TwoPinParts);

        Assert.Single(result.Diagnostics);
        Assert.True(result.Library.TryGet("widget", SymbolStyle.Iec, out var symbol));

        Assert.Equal(
            [
                new SymbolLine(new GridVector(-2, 0), new GridVector(2, 0)),
                new SymbolPolyline(
                    [new GridVector(0, 0), new GridVector(1, 0), new GridVector(1, 1)], true),
                new SymbolRect(-1, -0.4, 2, 0.8),
                new SymbolCircle(GridVector.Zero, 1, false),
                new SymbolCircle(GridVector.Zero, 0.2, true),
                new SymbolArc(GridVector.Zero, 0.3, 180, 180),
                new SymbolPolygon(
                    [new GridVector(0, 0), new GridVector(1, 0), new GridVector(0, 1)], true),
                new SymbolText(new GridVector(0.5, -0.5), "+"),
            ],
            symbol.Primitives);

        Assert.Equal(new GridVector(0, -1.2), symbol.DesignatorAnchor);
        Assert.Equal(new GridVector(0, 1.2), symbol.ValueAnchor);
    }

    [Fact]
    public void LabelValueAnchorIsOptional()
    {
        var json = SymbolJson.Valid().With("labels", """{ "designator": [0, -1.2] }""").ToString();

        var result = SymbolLibraryLoader.Load(
            new InMemoryContentSource(new ContentFile("widget.iec.json", json)),
            SymbolCatalogs.TwoPinParts);

        Assert.True(result.Library.TryGet("widget", SymbolStyle.Iec, out var symbol));
        Assert.Null(symbol.ValueAnchor);
    }

    [Fact]
    public void UnknownComponentAndStylePairIsNotFound()
    {
        var library = new SymbolLibrary([Symbol("widget")]);

        Assert.False(library.TryGet("gadget", SymbolStyle.Iec, out var symbol));
        Assert.Null(symbol);
    }

    [Fact]
    public void SymbolsAreOrderedByComponentId()
    {
        var library = new SymbolLibrary([Symbol("widget"), Symbol("gadget")]);

        Assert.Equal(["gadget", "widget"], library.Symbols.Select(entry => entry.ComponentId));
        Assert.Equal(2, library.Count);
    }

    [Fact]
    public void DuplicateComponentAndStylePairIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new SymbolLibrary([Symbol("widget"), Symbol("widget")]));
    }

    [Fact]
    public void EmptyLibraryHoldsNoSymbol()
    {
        Assert.Equal(0, SymbolLibrary.Empty.Count);
        Assert.Empty(SymbolLibrary.Empty.Symbols);
    }

    [Fact]
    public void SymbolsWithEqualContentsAreEqual()
    {
        Assert.Equal(Symbol("widget"), Symbol("widget"));
        Assert.Equal(Symbol("widget").GetHashCode(), Symbol("widget").GetHashCode());
        Assert.NotEqual(Symbol("widget"), Symbol("gadget"));
    }

    private static Symbol Symbol(string componentId) => new(
        componentId,
        SymbolStyle.Iec,
        [new SymbolLine(new GridVector(-2, 0), new GridVector(2, 0))],
        new GridVector(0, -1.2),
        new GridVector(0, 1.2));
}
