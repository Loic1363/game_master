using System.Collections.Immutable;
using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Nodal.Content.Symbols;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Content.Tests.Components;

public class StructuralRecordEqualityTests
{
    [Fact]
    public void DefinitionsWithEqualPinsAndParametersAreEqual()
    {
        Assert.Equal(Definition(), Definition());
        Assert.Equal(Definition().GetHashCode(), Definition().GetHashCode());
    }

    [Fact]
    public void DefinitionsDifferingInPinsOrParametersAreNotEqual()
    {
        Assert.NotEqual(Definition(), Definition() with { Pins = [new PinDefinition("1", 0, 0)] });
        Assert.NotEqual(Definition(), Definition() with { Parameters = [] });
        Assert.NotEqual(Definition(), Definition() with { Name = "Other" });
    }

    [Fact]
    public void DefinitionsDifferingOnlyInPinOrderAreNotEqual()
    {
        var reordered = Definition() with
        {
            Pins = [new PinDefinition("2", 2, 0), new PinDefinition("1", -2, 0)],
        };

        Assert.NotEqual(Definition(), reordered);
    }

    [Fact]
    public void TwoLoadsOfTheSameFilesProduceEqualDefinitionsAndDiagnostics()
    {
        var first = LoadCatalog();
        var second = LoadCatalog();

        Assert.Equal(first.Catalog.Definitions, second.Catalog.Definitions);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
    }

    [Fact]
    public void ResultsOverTheSameCatalogCompareByDiagnostics()
    {
        var catalog = LoadCatalog().Catalog;
        var result = new ComponentCatalogLoadResult(catalog, [Diagnostic()]);
        var same = new ComponentCatalogLoadResult(catalog, [Diagnostic()]);

        Assert.Equal(result, same);
        Assert.Equal(result.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(result, result with { Diagnostics = [] });
        Assert.NotEqual(result, result with { Catalog = ComponentCatalog.Empty });
    }

    [Fact]
    public void SymbolResultsDifferingInDiagnosticsAreNotEqual()
    {
        var result = new SymbolLibraryLoadResult(SymbolLibrary.Empty, []);

        Assert.Equal(result, new SymbolLibraryLoadResult(SymbolLibrary.Empty, []));
        Assert.Equal(result.GetHashCode(), new SymbolLibraryLoadResult(SymbolLibrary.Empty, []).GetHashCode());
        Assert.NotEqual(result, result with { Diagnostics = [Diagnostic()] });
    }

    [Fact]
    public void LoadedDefinitionsCarryImmutableArrays()
    {
        var catalog = LoadCatalog().Catalog;

        Assert.True(catalog.TryGetById("resistor", out var resistor));
        Assert.False(resistor.Pins.IsDefault);
        Assert.False(resistor.Parameters.IsDefault);
        Assert.Equal(2, resistor.Pins.Length);
        Assert.Single(resistor.Parameters);
    }

    private static ComponentCatalogLoadResult LoadCatalog() => ComponentCatalogLoader.Load(
        new InMemoryContentSource(new ContentFile("resistor.json", ResistorJson)));

    private static ContentDiagnostic Diagnostic() =>
        ContentDiagnostic.Error("resistor.json", "$", "Something is wrong.");

    private static ComponentDefinition Definition() => new(
        "resistor",
        "Resistor",
        "Linear two-terminal resistor.",
        ComponentCategory.Passive,
        "R",
        [new PinDefinition("1", -2, 0), new PinDefinition("2", 2, 0)],
        [Parameter()]);

    private static ParameterDefinition Parameter() => new(
        "resistance", Unit.Ohm, new Quantity(1000, Unit.Ohm), StandardSeries.E24);

    private const string ResistorJson = """
        {
          "schemaVersion": 1,
          "kind": "component",
          "id": "resistor",
          "name": "Resistor",
          "description": "Linear two-terminal resistor.",
          "category": "passive",
          "designatorPrefix": "R",
          "pins": [
            { "name": "1", "x": -2, "y": 0 },
            { "name": "2", "x": 2, "y": 0 }
          ],
          "parameters": [
            { "name": "resistance", "unit": "ohm", "default": "1k", "series": "E24" }
          ]
        }
        """;
}
