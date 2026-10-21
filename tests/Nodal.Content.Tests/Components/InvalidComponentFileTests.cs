using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Xunit;

namespace Nodal.Content.Tests.Components;

public class InvalidComponentFileTests
{
    private const string BrokenFileName = "broken.json";
    private const string GoodFileName = "good.json";

    public static TheoryData<string, string, string> InvalidFiles
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            Add(data, Broken("schemaVersion", "2"), "$.schemaVersion", "Unsupported schema version");
            Add(data, Broken("kind", Text("challenge")), "$.kind", "must declare kind 'component'");
            Add(data, Broken("id", Text("Widget")), "$.id", "only lowercase letters, digits and hyphens");
            Add(data, Broken("name", Text("  ")), "$.name", "must not be empty");
            Add(data, Broken("description", Text("")), "$.description", "must not be empty");
            Add(data, Broken("category", Text("exotic")), "$.category", "Unknown category 'exotic'");
            Add(data, Broken("designatorPrefix", Text("w")), "$.designatorPrefix", "uppercase letters");
            Add(data, Broken("pins", "[]"), "$.pins", "at least one pin");
            Add(
                data,
                Broken("pins", """[{ "name": "", "x": -2, "y": 0 }, { "name": "2", "x": 2, "y": 0 }]"""),
                "$.pins[0].name",
                "must not be empty");
            Add(
                data,
                Broken("pins", """[{ "name": "1", "x": -2, "y": 0 }, { "name": "1", "x": 2, "y": 0 }]"""),
                "$.pins",
                "The pin name '1' is used more than once.");
            Add(
                data,
                Broken("pins", """[{ "name": "1", "x": -2, "y": 0 }, { "name": "2", "x": -2, "y": 0 }]"""),
                "$.pins",
                "Two pins share the position (-2, 0).");
            Add(
                data,
                Broken("pins", """[{ "name": "1", "x": 1.5, "y": 0 }, { "name": "2", "x": 2, "y": 0 }]"""),
                "$.pins[0].x",
                "must be a whole number");
            Add(
                data,
                Broken("pins", """[5, { "name": "2", "x": 2, "y": 0 }]"""),
                "$.pins[0]",
                "must be a JSON object");
            Add(
                data,
                Broken("pins", """[{ "name": "1", "x": -2, "y": 0, "z": 0 }, { "name": "2", "x": 2, "y": 0 }]"""),
                "$.pins[0].z",
                "Unknown field 'z'");
            Add(data, Broken("pins", "{}"), "$.pins", "must be an array");
            Add(
                data,
                Broken("parameters", """[{ "name": "Resistance", "unit": "ohm", "default": "1k" }]"""),
                "$.parameters[0].name",
                "must start with a lowercase letter");
            Add(
                data,
                Broken("parameters", """[{ "name": "charge", "unit": "coulomb", "default": "1" }]"""),
                "$.parameters[0].unit",
                "Unknown unit 'coulomb'");
            Add(
                data,
                Broken("parameters", """[{ "name": "resistance", "unit": "ohm", "default": "oops" }]"""),
                "$.parameters[0].default",
                "is not a valid value for this unit");
            Add(
                data,
                Broken("parameters", """[{ "name": "resistance", "unit": "ohm", "default": "1k", "series": "E3" }]"""),
                "$.parameters[0].series",
                "Unknown series 'E3'");
            Add(
                data,
                Broken("parameters", """[{ "name": "resistance", "unit": "ohm", "default": "1k2", "series": "E6" }]"""),
                "$.parameters[0].default",
                "does not belong to series E6");
            Add(
                data,
                Broken(
                    "parameters",
                    """
                    [{ "name": "resistance", "unit": "ohm", "default": "1k" },
                     { "name": "resistance", "unit": "ohm", "default": "2k" }]
                    """),
                "$.parameters",
                "The parameter name 'resistance' is used more than once.");
            Add(
                data,
                Broken(
                    "parameters",
                    """[{ "name": "resistance", "unit": "ohm", "default": "1k", "tolerance": "1%" }]"""),
                "$.parameters[0].tolerance",
                "Unknown field 'tolerance'");
            Add(data, Broken("flavour", Text("salty")), "$.flavour", "Unknown field 'flavour'");
            Add(data, Missing("name"), "$.name", "Required field 'name' is missing");
            Add(data, Missing("pins"), "$.pins", "Required field 'pins' is missing");
            Add(data, Missing("parameters"), "$.parameters", "Required field 'parameters' is missing");
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
        var result = ComponentCatalogLoader.Load(new InMemoryContentSource(
            new ContentFile(BrokenFileName, brokenJson),
            new ContentFile(GoodFileName, ComponentJson.Valid().With("id", Text("good")).ToString())));

        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(ContentDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(BrokenFileName, diagnostic.SourceFile);
        Assert.Equal(expectedPath, diagnostic.Path);
        Assert.Contains(expectedMessagePart, diagnostic.Message, StringComparison.Ordinal);

        Assert.Equal(["good"], result.Catalog.Definitions.Select(definition => definition.Id));
    }

    [Fact]
    public void SecondFileWithTheSameIdIsReportedAndExcluded()
    {
        var result = ComponentCatalogLoader.Load(new InMemoryContentSource(
            new ContentFile("first.json", ComponentJson.Valid().ToString()),
            new ContentFile("second.json", ComponentJson.Valid().ToString())));

        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal("second.json", diagnostic.SourceFile);
        Assert.Equal("$.id", diagnostic.Path);
        Assert.Contains("already defined by first.json", diagnostic.Message, StringComparison.Ordinal);

        Assert.Equal(["widget"], result.Catalog.Definitions.Select(definition => definition.Id));
    }

    [Fact]
    public void EmptyParameterListIsValid()
    {
        var result = ComponentCatalogLoader.Load(new InMemoryContentSource(
            new ContentFile("empty.json", ComponentJson.Valid().With("parameters", "[]").ToString())));

        Assert.Empty(result.Diagnostics);
        Assert.Empty(Assert.Single(result.Catalog.Definitions).Parameters);
    }

    [Fact]
    public void EmptySourceLoadsAnEmptyCatalog()
    {
        var result = ComponentCatalogLoader.Load(new InMemoryContentSource());

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Catalog.Definitions);
    }

    private static void Add(
        TheoryData<string, string, string> data, string brokenJson, string path, string messagePart)
    {
        data.Add(brokenJson, path, messagePart);
    }

    private static string Broken(string field, string rawJsonValue) =>
        ComponentJson.Valid().With(field, rawJsonValue).ToString();

    private static string Missing(string field) => ComponentJson.Valid().Without(field).ToString();

    private static string Text(string value) => ComponentJson.Text(value);
}
