namespace Nodal.Circuit.Tests.Persistence;

internal sealed class SchematicJson
{
    private readonly List<KeyValuePair<string, string>> _fields;

    private SchematicJson(IEnumerable<KeyValuePair<string, string>> fields) => _fields = fields.ToList();

    public static SchematicJson Valid() =>
        new(
        [
            new("schemaVersion", "1"),
            new("kind", Text("schematic")),
            new("createdWith", Text("Nodal 0.0.1")),
            new("nextComponentId", "3"),
            new("nextWireId", "2"),
            new(
                "components",
                """
                [
                  { "id": 1, "definition": "resistor", "designator": "R1", "x": 0, "y": 0,
                    "rotation": 0, "mirrored": false, "parameters": { "resistance": 1000 } },
                  { "id": 2, "definition": "ground", "designator": "GND1", "x": 0, "y": 4,
                    "rotation": 180, "mirrored": true, "parameters": {} }
                ]
                """),
            new("wires", """[{ "id": 1, "from": [0, 2], "to": [0, 4] }]"""),
        ]);

    public static SchematicJson Single(string definitionId, string designator, string parameters) =>
        Valid()
            .With(
                "components",
                $$"""
                [
                  { "id": 1, "definition": "{{definitionId}}", "designator": "{{designator}}",
                    "x": 0, "y": 0, "rotation": 0, "mirrored": false, "parameters": {{parameters}} }
                ]
                """)
            .With("nextComponentId", "2")
            .With("nextWireId", "1")
            .With("wires", "[]");

    public static string Text(string value) => "\"" + value + "\"";

    public SchematicJson With(string field, string rawJsonValue)
    {
        var replaced = new SchematicJson(_fields);
        var index = replaced._fields.FindIndex(entry => entry.Key == field);

        if (index < 0)
        {
            replaced._fields.Add(new KeyValuePair<string, string>(field, rawJsonValue));
        }
        else
        {
            replaced._fields[index] = new KeyValuePair<string, string>(field, rawJsonValue);
        }

        return replaced;
    }

    public SchematicJson Without(string field)
    {
        var replaced = new SchematicJson(_fields);
        replaced._fields.RemoveAll(entry => entry.Key == field);

        return replaced;
    }

    public override string ToString() =>
        "{" + string.Join(",", _fields.Select(entry => Text(entry.Key) + ":" + entry.Value)) + "}";
}
