namespace Nodal.Content.Symbols;

internal static class SymbolFields
{
    public const string SchemaVersion = "schemaVersion";
    public const string Kind = "kind";
    public const string Component = "component";
    public const string Style = "style";
    public const string Primitives = "primitives";
    public const string Type = "type";
    public const string From = "from";
    public const string To = "to";
    public const string Points = "points";
    public const string Closed = "closed";
    public const string X = "x";
    public const string Y = "y";
    public const string Width = "width";
    public const string Height = "height";
    public const string Center = "center";
    public const string Radius = "radius";
    public const string Filled = "filled";
    public const string StartDegrees = "startDegrees";
    public const string SweepDegrees = "sweepDegrees";
    public const string At = "at";
    public const string Value = "value";

    public static readonly string[] Root = [SchemaVersion, Kind, Component, Style, Primitives];

    public static readonly string[] Line = [Type, From, To];

    public static readonly string[] Polyline = [Type, Points, Closed];

    public static readonly string[] Rect = [Type, X, Y, Width, Height];

    public static readonly string[] Circle = [Type, Center, Radius, Filled];

    public static readonly string[] Arc = [Type, Center, Radius, StartDegrees, SweepDegrees];

    public static readonly string[] Polygon = [Type, Points, Filled];

    public static readonly string[] Text = [Type, At, Value];
}
