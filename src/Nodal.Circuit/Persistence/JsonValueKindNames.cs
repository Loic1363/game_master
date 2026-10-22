using System.Text.Json;

namespace Nodal.Circuit.Persistence;

internal static class JsonValueKindNames
{
    private static readonly Dictionary<JsonValueKind, string> NameByKind = new()
    {
        [JsonValueKind.String] = "a string",
        [JsonValueKind.Number] = "a number",
        [JsonValueKind.Array] = "an array",
        [JsonValueKind.Object] = "an object",
    };

    public static string Of(JsonValueKind kind) =>
        NameByKind.TryGetValue(kind, out var name)
            ? name
            : throw new ArgumentOutOfRangeException(
                nameof(kind), kind, "No readable name for this JSON value kind.");
}
