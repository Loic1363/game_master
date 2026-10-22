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
            Add(data, BrokenParameter(Named("Resistance")), "$.parameters[0].name", LowercaseWords);
            Add(data, BrokenParameter(Named("voltage--rating")), "$.parameters[0].name", LowercaseWords);
            Add(data, BrokenParameter(Named("voltage-")), "$.parameters[0].name", LowercaseWords);
            Add(
                data,
                BrokenParameter(Parameter(Name, Tier, Ohm)),
                "$.parameters[0].label",
                "Required field 'label' is missing");
            Add(
                data,
                BrokenParameter(Parameter(Name, Field("label", "  "), Tier, Ohm)),
                "$.parameters[0].label",
                "must not be empty");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Ohm)),
                "$.parameters[0].tier",
                "Required field 'tier' is missing");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Field("tier", "expert"), Ohm)),
                "$.parameters[0].tier",
                "Unknown tier 'expert'");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Field("unit", "coulomb"))),
                "$.parameters[0].unit",
                "Unknown unit 'coulomb'");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Ohm, Field("default", "oops"))),
                "$.parameters[0].default",
                "is not a valid value for this unit");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Ohm, Default, Field("series", "E3"))),
                "$.parameters[0].series",
                "Unknown series 'E3'");
            Add(
                data,
                BrokenParameter(
                    Parameter(Name, Label, Tier, Ohm, Field("default", "1k2"), Field("series", "E6"))),
                "$.parameters[0].default",
                "does not belong to series E6");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Ohm, Default, Field("min", "2k"))),
                "$.parameters[0].default",
                "is below the minimum");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Ohm, Default, Field("max", "500"))),
                "$.parameters[0].default",
                "is above the maximum");
            Add(
                data,
                BrokenParameter(
                    Parameter(Name, Label, Tier, Ohm, Field("min", "2k"), Field("max", "1k"))),
                "$.parameters[0].min",
                "must not be greater than the maximum");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Ohm, Field("min", "5 V"))),
                "$.parameters[0].min",
                "is not a valid value for this unit");
            Add(
                data,
                BrokenParameter(Parameter(Name, Label, Tier, Ohm, Default, Field("max", "5 V"))),
                "$.parameters[0].max",
                "is not a valid value for this unit");
            Add(
                data,
                Broken("parameters", "[" + ValidParameter + ", " + ValidParameter + "]"),
                "$.parameters",
                "The parameter name 'resistance' is used more than once.");
            Add(
                data,
                BrokenParameter(
                    Parameter(Name, Label, Tier, Ohm, Default, Field("tolerance", "1%"))),
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

    private const string LowercaseWords = "must start with a lowercase letter";

    private static readonly string Name = Field("name", "resistance");

    private static readonly string Label = Field("label", "Resistance");

    private static readonly string Tier = Field("tier", "basic");

    private static readonly string Ohm = Field("unit", "ohm");

    private static readonly string Default = Field("default", "1k");

    private static readonly string ValidParameter = Parameter(Name, Label, Tier, Ohm, Default);

    private static string Field(string name, string value) => Text(name) + ": " + Text(value);

    private static string Parameter(params string[] fields) => "{ " + string.Join(", ", fields) + " }";

    private static string Named(string name) =>
        Parameter(Field("name", name), Label, Tier, Ohm, Default);

    private static string BrokenParameter(string parameterJson) =>
        Broken("parameters", "[" + parameterJson + "]");

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
