using System.Globalization;

namespace Nodal.Content.Components;

internal static class JsonPath
{
    public const string Root = "$";

    public static string Field(string path, string fieldName) => path + "." + fieldName;

    public static string Element(string path, int index) =>
        path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
}
