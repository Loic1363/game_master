using Nodal.Content.Components;
using Nodal.Content.Sources;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Content.Tests.Symbols;

public class BaseSymbolLibraryTests
{
    private const int ExpectedSymbolCount = 11;
    private const double MarginInGridUnits = 1.5;

    private static readonly string[] ExpectedComponentIds =
    [
        "capacitor", "current-source", "diode", "ground", "inductor", "nmos",
        "npn", "pmos", "pnp", "resistor", "voltage-source",
    ];

    public static TheoryData<string> ComponentIds => new(ExpectedComponentIds);

    [Fact]
    public void BaseSymbolLibraryLoadsWithoutDiagnostics()
    {
        var result = LoadBaseLibrary();

        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExpectedSymbolCount, result.Library.Count);
    }

    [Fact]
    public void BaseSymbolLibraryCoversEveryComponentInIecStyle()
    {
        var library = LoadBaseLibrary().Library;

        Assert.Equal(ExpectedComponentIds, library.Symbols.Select(symbol => symbol.ComponentId));
        Assert.All(library.Symbols, symbol => Assert.Equal(SymbolStyle.Iec, symbol.Style));
    }

    [Theory]
    [MemberData(nameof(ComponentIds))]
    public void BaseSymbolFitsWithinItsPinsExtentAndMargin(string componentId)
    {
        var symbol = SymbolOf(componentId);
        var definition = DefinitionOf(componentId);

        var minimumX = definition.Pins.Min(pin => (double)pin.X) - MarginInGridUnits;
        var maximumX = definition.Pins.Max(pin => (double)pin.X) + MarginInGridUnits;
        var minimumY = definition.Pins.Min(pin => (double)pin.Y) - MarginInGridUnits;
        var maximumY = definition.Pins.Max(pin => (double)pin.Y) + MarginInGridUnits;

        foreach (var primitive in symbol.Primitives)
        {
            foreach (var point in Extremes(primitive))
            {
                Assert.InRange(point.X, minimumX, maximumX);
                Assert.InRange(point.Y, minimumY, maximumY);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ComponentIds))]
    public void BaseSymbolEndsALeadOnEveryPin(string componentId)
    {
        var symbol = SymbolOf(componentId);
        var ends = LeadEnds(symbol).ToArray();

        foreach (var pin in DefinitionOf(componentId).Pins)
        {
            Assert.Contains(ends, end => end.X == pin.X && end.Y == pin.Y);
        }
    }

    private static IEnumerable<GridVector> Extremes(SymbolPrimitive primitive)
    {
        switch (primitive)
        {
            case SymbolLine line:
                yield return line.From;
                yield return line.To;
                break;

            case SymbolPolyline polyline:
                foreach (var point in polyline.Points)
                {
                    yield return point;
                }

                break;

            case SymbolPolygon polygon:
                foreach (var point in polygon.Points)
                {
                    yield return point;
                }

                break;

            case SymbolRect rect:
                yield return new GridVector(rect.X, rect.Y);
                yield return new GridVector(rect.X + rect.Width, rect.Y + rect.Height);
                break;

            case SymbolCircle circle:
                yield return circle.Center - new GridVector(circle.Radius, circle.Radius);
                yield return circle.Center + new GridVector(circle.Radius, circle.Radius);
                break;

            case SymbolArc arc:
                yield return arc.Center - new GridVector(arc.Radius, arc.Radius);
                yield return arc.Center + new GridVector(arc.Radius, arc.Radius);
                break;

            case SymbolText text:
                yield return text.At;
                break;
        }
    }

    private static IEnumerable<GridVector> LeadEnds(Symbol symbol)
    {
        foreach (var primitive in symbol.Primitives)
        {
            switch (primitive)
            {
                case SymbolLine line:
                    yield return line.From;
                    yield return line.To;
                    break;

                case SymbolPolyline polyline:
                    foreach (var point in polyline.Points)
                    {
                        yield return point;
                    }

                    break;
            }
        }
    }

    private static Symbol SymbolOf(string componentId)
    {
        Assert.True(LoadBaseLibrary().Library.TryGet(componentId, SymbolStyle.Iec, out var symbol));

        return symbol;
    }

    private static ComponentDefinition DefinitionOf(string componentId)
    {
        Assert.True(BaseCatalog().TryGetById(componentId, out var definition));

        return definition;
    }

    private static SymbolLibraryLoadResult LoadBaseLibrary() => SymbolLibraryLoader.Load(
        new DirectoryContentSource(RepositoryPaths.SymbolsDirectory), BaseCatalog());

    private static ComponentCatalog BaseCatalog() =>
        ComponentCatalogLoader.Load(
            new DirectoryContentSource(RepositoryPaths.ComponentsDirectory)).Catalog;
}
