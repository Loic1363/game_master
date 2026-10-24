using Godot;

namespace Nodal.Game.Rendering;

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
}
