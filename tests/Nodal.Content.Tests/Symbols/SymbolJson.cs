namespace Nodal.Content.Tests.Symbols;

internal sealed class SymbolJson
{
    private readonly List<KeyValuePair<string, string>> _fields;

    private SymbolJson(IEnumerable<KeyValuePair<string, string>> fields)
    {
        _fields = fields.ToList();
    }

    public static SymbolJson Valid() =>
        new(
        [
            new("schemaVersion", "1"),
            new("kind", Text("symbol")),
            new("component", Text("widget")),
            new("style", Text("iec")),
            new("primitives", """[{ "type": "line", "from": [-2, 0], "to": [2, 0] }]"""),
            new("labels", """{ "designator": [0, -1.2], "value": [0, 1.2] }"""),
        ]);

    public static string Text(string value) => "\"" + value + "\"";

    public SymbolJson With(string field, string rawJsonValue)
    {
        var replaced = new SymbolJson(_fields);
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

    public SymbolJson Without(string field)
    {
        var replaced = new SymbolJson(_fields);
        replaced._fields.RemoveAll(entry => entry.Key == field);

        return replaced;
    }

    public SymbolJson WithPrimitive(string rawJsonObject) =>
        With("primitives", "[" + Lead() + ", " + rawJsonObject + "]");

    public override string ToString() =>
        "{" + string.Join(",", _fields.Select(entry => Text(entry.Key) + ":" + entry.Value)) + "}";

    private static string Lead() => """{ "type": "line", "from": [-2, 0], "to": [2, 0] }""";
}
