using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Nodal.Content.Symbols;
using Xunit;

namespace Nodal.Content.Tests.Symbols;

public class InvalidSymbolFileTests
{
    private const string BrokenFileName = "widget.iec.json";
    private const string GoodFileName = "gadget.iec.json";

    public static TheoryData<string, string, string> InvalidFiles
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            Add(data, Broken("schemaVersion", "2"), "$.schemaVersion", "Unsupported schema version");
            Add(data, Broken("kind", Text("component")), "$.kind", "must declare kind 'symbol'");
            Add(data, Missing("kind"), "$.kind", "Required field 'kind' is missing");
            Add(data, Broken("component", Text("Widget")), "$.component", "only lowercase letters");
            Add(
                data,
                Broken("component", Text("sprocket")),
                "$.component",
                "No component carries the id 'sprocket'.");
            Add(data, Broken("style", Text("ansi")), "$.style", "Unknown style 'ansi'");
            Add(data, Broken("flavour", Text("salty")), "$.flavour", "Unknown field 'flavour'");
            Add(data, Broken("primitives", "{}"), "$.primitives", "must be an array");
            Add(data, Broken("primitives", "[7]"), "$.primitives[0]", "must be a JSON object");
            Add(
                data,
                Broken("primitives", """[{ "type": "spline", "from": [0, 0], "to": [1, 1] }]"""),
                "$.primitives[0].type",
                "Unknown primitive type 'spline'");
            Add(
                data,
                Broken("primitives", """[{ "type": "line", "from": [-2, 0] }]"""),
                "$.primitives[0].to",
                "Required field 'to' is missing");
            Add(
                data,
                Broken("primitives", """[{ "type": "line", "from": [-20, 0], "to": [2, 0] }]"""),
                "$.primitives[0].from[0]",
                "within 10 grid units");
            Add(
                data,
                Broken("primitives", """[{ "type": "line", "from": ["x", 0], "to": [2, 0] }]"""),
                "$.primitives[0].from[0]",
                "must be a number");
            Add(
                data,
                Broken("primitives", """[{ "type": "line", "from": [0, 0, 0], "to": [2, 0] }]"""),
                "$.primitives[0].from",
                "array of two numbers");
            Add(
                data,
                Broken("primitives", """[{ "type": "line", "from": [-2, 0], "to": [2, 0], "dashed": true }]"""),
                "$.primitives[0].dashed",
                "Unknown field 'dashed'");
            Add(
                data,
                WithPrimitive("""{ "type": "polyline", "points": [[0, 0], [1, 0]] }"""),
                "$.primitives[1].closed",
                "Required field 'closed' is missing");
            Add(
                data,
                WithPrimitive("""{ "type": "polyline", "points": [[0, 0]], "closed": false }"""),
                "$.primitives[1].points",
                "at least 2 points");
            Add(
                data,
                WithPrimitive("""{ "type": "rect", "x": 0, "y": 0, "width": 0, "height": 1 }"""),
                "$.primitives[1].width",
                "greater than 0");
            Add(
                data,
                WithPrimitive("""{ "type": "circle", "center": [0, 0], "radius": 1, "filled": "yes" }"""),
                "$.primitives[1].filled",
                "must be true or false");
            Add(
                data,
                WithPrimitive(
                    """{ "type": "arc", "center": [0, 0], "radius": 1, "startDegrees": 0, "sweepDegrees": 0 }"""),
                "$.primitives[1].sweepDegrees",
                "must not be zero");
            Add(
                data,
                WithPrimitive("""{ "type": "polygon", "points": [[0, 0], [1, 0], [0, 1]], "filled": 1 }"""),
                "$.primitives[1].filled",
                "must be true or false");
            Add(
                data,
                WithPrimitive("""{ "type": "text", "at": [0, 0], "value": " " }"""),
                "$.primitives[1].value",
                "must not be empty");
            Add(data, Missing("labels"), "$.labels", "Required field 'labels' is missing");
            Add(
                data,
                Broken("labels", """{ "value": [0, 1.2] }"""),
                "$.labels.designator",
                "Required field 'designator' is missing");
            Add(
                data,
                Broken("labels", """{ "designator": [0, -1.2], "middle": [0, 0] }"""),
                "$.labels.middle",
                "Unknown field 'middle'");
            Add(
                data,
                Broken("primitives", """[{ "type": "line", "from": [-2, 0], "to": [0, 0] }]"""),
                "$.primitives",
                "No lead ends on the pin '2' at (2, 0).");
            Add(data, """{ "schemaVersion": }""", "$", "not valid JSON");
            Add(data, "[]", "$", "must contain a JSON object");

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidFiles))]
    public void InvalidFileIsReportedAndExcludedWhileValidFilesStillLoad(
        string brokenJson, string expectedPath, string expectedMessagePart)
    {
        var result = SymbolLibraryLoader.Load(
            new InMemoryContentSource(
                new ContentFile(BrokenFileName, brokenJson),
                new ContentFile(GoodFileName, Gadget())),
            SymbolCatalogs.TwoPinParts);

        var error = Assert.Single(Errors(result));

        Assert.Equal(BrokenFileName, error.SourceFile);
        Assert.Equal(expectedPath, error.Path);
        Assert.Contains(expectedMessagePart, error.Message, StringComparison.Ordinal);

        Assert.Equal(["gadget"], result.Library.Symbols.Select(symbol => symbol.ComponentId));

        var warning = Assert.Single(Warnings(result));

        Assert.Contains("widget", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondSymbolForTheSameComponentAndStyleIsReportedAndExcluded()
    {
        var result = SymbolLibraryLoader.Load(
            new InMemoryContentSource(
                new ContentFile("first.json", Gadget()),
                new ContentFile("second.json", Gadget())),
            SymbolCatalogs.TwoPinParts);

        var error = Assert.Single(Errors(result));

        Assert.Equal("second.json", error.SourceFile);
        Assert.Contains("already defined by first.json", error.Message, StringComparison.Ordinal);

        Assert.Equal(["gadget"], result.Library.Symbols.Select(symbol => symbol.ComponentId));
    }

    [Fact]
    public void ComponentWithoutAnIecSymbolIsReportedAsAWarning()
    {
        var result = SymbolLibraryLoader.Load(new InMemoryContentSource(), SymbolCatalogs.TwoPinParts);

        Assert.Empty(Errors(result));
        Assert.Equal(0, result.Library.Count);

        var warnings = Warnings(result);

        Assert.Equal(["gadget.iec.json", "widget.iec.json"], warnings.Select(warning => warning.SourceFile));
        Assert.All(warnings, warning => Assert.Contains("iec symbol", warning.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void EmptySourceWithAnEmptyCatalogLoadsAnEmptyLibrary()
    {
        var result = SymbolLibraryLoader.Load(new InMemoryContentSource(), ComponentCatalog.Empty);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(0, result.Library.Count);
    }

    private static ContentDiagnostic[] Errors(SymbolLibraryLoadResult result) =>
        [.. result.Diagnostics.Where(
            diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)];

    private static ContentDiagnostic[] Warnings(SymbolLibraryLoadResult result) =>
        [.. result.Diagnostics.Where(
            diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Warning)];

    private static void Add(
        TheoryData<string, string, string> data, string brokenJson, string path, string messagePart)
    {
        data.Add(brokenJson, path, messagePart);
    }

    private static string Broken(string field, string rawJsonValue) =>
        SymbolJson.Valid().With(field, rawJsonValue).ToString();

    private static string WithPrimitive(string rawJsonObject) =>
        SymbolJson.Valid().WithPrimitive(rawJsonObject).ToString();

    private static string Missing(string field) => SymbolJson.Valid().Without(field).ToString();

    private static string Gadget() =>
        SymbolJson.Valid().With("component", Text("gadget")).ToString();

    private static string Text(string value) => SymbolJson.Text(value);
}
