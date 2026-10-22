namespace Nodal.Content.Tests.Components;

internal sealed class ComponentJson
{
    private readonly List<KeyValuePair<string, string>> _fields;

    private ComponentJson(IEnumerable<KeyValuePair<string, string>> fields)
    {
        _fields = fields.ToList();
    }

    public static ComponentJson Valid() =>
        new(
        [
            new("schemaVersion", "1"),
            new("kind", Text("component")),
            new("id", Text("widget")),
            new("name", Text("Widget")),
            new("description", Text("A widget used by the tests.")),
            new("category", Text("passive")),
            new("designatorPrefix", Text("W")),
            new("pins", """[{ "name": "1", "x": -2, "y": 0 }, { "name": "2", "x": 2, "y": 0 }]"""),
            new(
                "parameters",
                """
                [{ "name": "resistance", "label": "Resistance", "tier": "basic",
                   "unit": "ohm", "default": "1k", "series": "E24" }]
                """),
        ]);

    public static string Text(string value) => "\"" + value + "\"";

    public ComponentJson With(string field, string rawJsonValue)
    {
        var replaced = new ComponentJson(_fields);
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

    public ComponentJson Without(string field)
    {
        var replaced = new ComponentJson(_fields);
        replaced._fields.RemoveAll(entry => entry.Key == field);

        return replaced;
    }

    public override string ToString() =>
        "{" + string.Join(",", _fields.Select(entry => Text(entry.Key) + ":" + entry.Value)) + "}";
}
