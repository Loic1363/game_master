using Godot;
using Nodal.Circuit.Symbols;

namespace Nodal.Game.Rendering;

public static class CanvasText
{
    public static int FontSize(float zoom) => (int)Math.Clamp(
        Math.Round(LabelPlacement.LineHeight * CanvasMetrics.GridPixelsAtUnitZoom * zoom),
        CanvasMetrics.MinimumFontSize,
        CanvasMetrics.MaximumFontSize);

    public static void DrawCentered(
        CanvasItem canvas, Font font, Vector2 center, string text, int fontSize, Color color) =>
        DrawAligned(canvas, font, center, LabelAlignment.Centered, text, fontSize, color);

    public static void DrawAligned(
        CanvasItem canvas,
        Font font,
        Vector2 anchor,
        LabelAlignment alignment,
        string text,
        int fontSize,
        Color color)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(font);

        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var ascent = font.GetAscent(fontSize);
        var descent = font.GetDescent(fontSize);

        var left = alignment == LabelAlignment.Centered ? anchor.X - (size.X / 2f) : anchor.X;
        var baseline = new Vector2(left, anchor.Y + ((ascent - descent) / 2f));

        canvas.DrawString(font, baseline, text, HorizontalAlignment.Left, -1, fontSize, color);
    }
}
