using Godot;

namespace Nodal.Game.Style;

public static class VisualTokens
{
    public static Color Background { get; } = Color.FromHtml("F7F5F0");

    public static Color GridDots { get; } = Color.FromHtml("D8D4CC");

    public static Color Stroke { get; } = Color.FromHtml("2B2B2B");

    public static Color LabelText { get; } = Color.FromHtml("4A4A4A");

    public static Color NetHighlight { get; } = Color.FromHtml("E07A1F");

    public static Color Selection { get; } = Color.FromHtml("2F6FDE");

    public static Color Error { get; } = Color.FromHtml("C0392B");

    public static Color LabelPill { get; } = Color.FromHtml("FFFDF8E6");

    public static Color PlotBackground { get; } = Color.FromHtml("FFFDF8");

    public static Color PlotGrid { get; } = Color.FromHtml("E4E0D8");

    public static Color PlotAxis { get; } = Color.FromHtml("A8A49C");

    public static Color PlotCursor { get; } = Color.FromHtml("7A7A7A");

    public static Color PlotMarker { get; } = Color.FromHtml("C0392B");

    public static Color ReadoutBackground { get; } = Color.FromHtml("FFFDF8F2");

    public static Color SplitterBackground { get; } = Color.FromHtml("E4E0D8");

    public static Color[] TracePalette { get; } =
    [
        Color.FromHtml("2F6FDE"),
        Color.FromHtml("E07A1F"),
        Color.FromHtml("3C8C4A"),
        Color.FromHtml("C0392B"),
        Color.FromHtml("7D4FBF"),
        Color.FromHtml("0F9BA8"),
        Color.FromHtml("B5A000"),
        Color.FromHtml("D1559B"),
    ];

    public static Color TraceColor(int index) =>
        TracePalette[((index % TracePalette.Length) + TracePalette.Length) % TracePalette.Length];
}
