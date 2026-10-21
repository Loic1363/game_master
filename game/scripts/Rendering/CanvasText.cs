using Godot;

namespace Nodal.Game.Rendering;

public static class CanvasText
{
    public static int FontSize(float zoom) => (int)Math.Clamp(
        Math.Round(CanvasMetrics.LabelHeightInGridUnits * CanvasMetrics.GridPixelsAtUnitZoom * zoom),
        CanvasMetrics.MinimumFontSize,
        CanvasMetrics.MaximumFontSize);

    public static void DrawCentered(
        CanvasItem canvas, Font font, Vector2 center, string text, int fontSize, Color color)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(font);

        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var ascent = font.GetAscent(fontSize);
        var descent = font.GetDescent(fontSize);

        var baseline = new Vector2(
            center.X - (size.X / 2f), center.Y + ((ascent - descent) / 2f));

        canvas.DrawString(font, baseline, text, HorizontalAlignment.Left, -1, fontSize, color);
    }
}
