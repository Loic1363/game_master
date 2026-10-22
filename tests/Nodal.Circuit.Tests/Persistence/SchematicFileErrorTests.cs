using Nodal.Circuit.Persistence;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class SchematicFileErrorTests
{
    private const string TwoResistors =
        """
        [
          { "id": 1, "definition": "resistor", "designator": "R1", "x": 0, "y": 0,
            "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } },
          { "id": 2, "definition": "resistor", "designator": "R2", "x": 6, "y": 0,
            "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } }
        ]
        """;

    private const string SameIdTwice =
        """
        [
          { "id": 1, "definition": "resistor", "designator": "R1", "x": 0, "y": 0,
            "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } },
          { "id": 1, "definition": "resistor", "designator": "R2", "x": 6, "y": 0,
            "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } }
        ]
        """;

    private const string SameDesignatorTwice =
        """
        [
          { "id": 1, "definition": "resistor", "designator": "R1", "x": 0, "y": 0,
            "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } },
          { "id": 2, "definition": "resistor", "designator": "R1", "x": 6, "y": 0,
            "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } }
        ]
        """;

    private const string SameWireIdTwice =
        """
        [
          { "id": 1, "from": [0, 2], "to": [0, 4] },
          { "id": 1, "from": [0, 4], "to": [4, 4] }
        ]
        """;

    public static TheoryData<string, string, string> RefusedFiles
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            data.Add("{ \"kind\": ", "$", "not valid JSON");
            data.Add("[]", "$", "must contain a JSON object");

            Add(data, Valid().With("kind", Text("challenge")), "$.kind", "must declare kind 'schematic'");
            Add(data, Valid().Without("kind"), "$.kind", "Required field 'kind' is missing");
            Add(data, Valid().With("kind", "7"), "$.kind", "must be a string");
            Add(data, Valid().With("schemaVersion", "2"), "$.schemaVersion", "Unsupported schema version");
            Add(
                data,
                Valid().Without("schemaVersion"),
                "$.schemaVersion",
                "Required field 'schemaVersion' is missing");

            Add(data, Valid().With("schemaVersion", Text("1")), "$.schemaVersion", "must be a number");
            Add(
                data,
                Valid().Without("components"),
                "$.components",
                "Required field 'components' is missing");

            Add(data, Valid().With("components", "{}"), "$.components", "must be an array");
            Add(data, Valid().Without("wires"), "$.wires", "Required field 'wires' is missing");
            Add(data, Valid().With("wires", "7"), "$.wires", "must be an array");
            Add(
                data,
                Valid().Without("nextComponentId"),
                "$.nextComponentId",
                "Required field 'nextComponentId' is missing");

            Add(data, Valid().With("nextWireId", Text("2")), "$.nextWireId", "must be a number");
            Add(data, Valid().With("nextComponentId", "0"), "$.nextComponentId", "positive whole number");
            Add(data, Valid().With("nextWireId", "1.5"), "$.nextWireId", "must be a whole number");
            Add(data, Valid().With("components", "[5]"), "$.components[0]", "must be a JSON object");
            Add(
                data,
                Valid().With("components", Component(without: "designator")),
                "$.components[0].designator",
                "Required field 'designator' is missing");

            Add(
                data,
                Valid().With("components", Component(designator: Text("   "))),
                "$.components[0].designator",
                "must not be empty");

            Add(
                data,
                Valid().With("components", Component(without: "definition")),
                "$.components[0].definition",
                "Required field 'definition' is missing");

            Add(
                data,
                Valid().With("components", Component(x: Text("0"))),
                "$.components[0].x",
                "must be a number");

            Add(
                data,
                Valid().With("components", Component(without: "y")),
                "$.components[0].y",
                "Required field 'y' is missing");

            Add(
                data,
                Valid().With("components", Component(id: "0")),
                "$.components[0].id",
                "positive whole number");

            Add(
                data,
                Valid().With("components", Component(rotation: "45")),
                "$.components[0].rotation",
                "must be 0, 90, 180 or 270 degrees, not 45");

            Add(
                data,
                Valid().With("components", Component(rotation: "360")),
                "$.components[0].rotation",
                "must be 0, 90, 180 or 270 degrees, not 360");

            Add(
                data,
                Valid().With("components", Component(without: "rotation")),
                "$.components[0].rotation",
                "Required field 'rotation' is missing");

            Add(
                data,
                Valid().With("components", Component(mirrored: Text("no"))),
                "$.components[0].mirrored",
                "must be true or false");

            Add(
                data,
                Valid().With("components", Component(parameters: "[]")),
                "$.components[0].parameters",
                "must be an object");

            Add(
                data,
                Valid().With("components", SameIdTwice).With("nextComponentId", "3"),
                "$.components",
                "The component id 1 appears more than once.");

            Add(
                data,
                Valid().With("components", SameDesignatorTwice).With("nextComponentId", "3"),
                "$.components",
                "The designator 'R1' appears more than once.");

            Add(
                data,
                Valid().With("wires", SameWireIdTwice).With("nextWireId", "3"),
                "$.wires",
                "The wire id 1 appears more than once.");

            Add(
                data,
                Valid().With("wires", """[{ "id": 1, "from": [0, 2], "to": [4, 4] }]"""),
                "$.wires[0]",
                "cannot be diagonal");

            Add(
                data,
                Valid().With("wires", """[{ "id": 1, "from": [0, 2], "to": [0, 2] }]"""),
                "$.wires[0]",
                "cannot have zero length");

            Add(
                data,
                Valid().With("wires", """[{ "id": 1, "to": [0, 2] }]"""),
                "$.wires[0].from",
                "Required field 'from' is missing");

            Add(
                data,
                Valid().With("wires", """[{ "id": 1, "from": [0, 2, 4], "to": [0, 4] }]"""),
                "$.wires[0].from",
                "two whole numbers");

            Add(
                data,
                Valid().With("wires", """[{ "id": 1, "from": ["0", 2], "to": [0, 4] }]"""),
                "$.wires[0].from[0]",
                "two whole numbers");

            Add(
                data,
                Valid().With("wires", """[{ "id": 1, "from": [0, 2.5], "to": [0, 4] }]"""),
                "$.wires[0].from[1]",
                "two whole numbers");

            Add(
                data,
                Valid().With("components", TwoResistors).With("nextComponentId", "2"),
                "$.nextComponentId",
                "must be greater than the used id 2");

            Add(
                data,
                Valid().With("nextWireId", "1"),
                "$.nextWireId",
                "must be greater than the used id 1");

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RefusedFiles))]
    public void ABrokenFileIsRefusedWithAnError(string text, string path, string messagePart)
    {
        var result = SchematicFile.Read(text, BaseCatalog.Catalog);

        Assert.False(result.Loaded);
        Assert.Null(result.Schematic);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == SchematicSeverity.Error
                && diagnostic.Path == path
                && diagnostic.Message.Contains(messagePart, StringComparison.Ordinal));
    }

    [Fact]
    public void AValidFileIsReadWithoutDiagnostics()
    {
        var result = SchematicFile.Read(Valid().ToString(), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.Schematic.Components.Count);
        Assert.Single(result.Schematic.Wires);
        Assert.Equal(3, result.Schematic.NextComponentId.Value);
        Assert.Equal(2, result.Schematic.NextWireId.Value);
    }

    [Fact]
    public void AnEmptyTextIsRefusedWithoutThrowing()
    {
        var result = SchematicFile.Read(string.Empty, BaseCatalog.Catalog);

        Assert.False(result.Loaded);
        Assert.Single(result.Errors);
    }

    private static void Add(
        TheoryData<string, string, string> data, SchematicJson json, string path, string messagePart) =>
        data.Add(json.ToString(), path, messagePart);

    private static SchematicJson Valid() => SchematicJson.Valid();

    private static string Text(string value) => SchematicJson.Text(value);

    private static string Component(
        string id = "1",
        string definition = "\"resistor\"",
        string designator = "\"R1\"",
        string x = "0",
        string y = "0",
        string rotation = "0",
        string mirrored = "false",
        string parameters = """{ "resistance": 1000 }""",
        string without = "")
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("id", id),
            new("definition", definition),
            new("designator", designator),
            new("x", x),
            new("y", y),
            new("rotation", rotation),
            new("mirrored", mirrored),
            new("parameters", parameters),
        };

        fields.RemoveAll(field => field.Key == without);

        return "[{" + string.Join(",", fields.Select(field => Text(field.Key) + ":" + field.Value)) + "}]";
    }
}
