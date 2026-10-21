namespace Nodal.Content.Symbols;

internal static class SymbolVocabulary
{
    public const string LineType = "line";
    public const string PolylineType = "polyline";
    public const string RectType = "rect";
    public const string CircleType = "circle";
    public const string ArcType = "arc";
    public const string PolygonType = "polygon";
    public const string TextType = "text";
    public const string SymbolKind = "symbol";
    public const string IecStyleName = "iec";

    private static readonly Dictionary<string, SymbolStyle> StyleByName =
        new(StringComparer.Ordinal) { [IecStyleName] = SymbolStyle.Iec };

    public static string StyleNames => string.Join(", ", StyleByName.Keys);

    public static string PrimitiveTypeNames => string.Join(
        ", ", LineType, PolylineType, RectType, CircleType, ArcType, PolygonType, TextType);

    public static bool TryStyle(string name, out SymbolStyle style) =>
        StyleByName.TryGetValue(name, out style);

    public static string NameOf(SymbolStyle style) => style switch
    {
        SymbolStyle.Iec => IecStyleName,
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown symbol style."),
    };
}
