using Godot;

namespace Nodal.Game.Style;

public static class WaveformStyle
{
    private const string SeparationConstant = "separation";
    private const string SplitBarStylebox = "split_bar_background";

    public static Color Background => VisualTokens.PlotBackground;

    public static Color Grid => VisualTokens.PlotGrid;

    public static Color Axis => VisualTokens.PlotAxis;

    public static Color AxisText => VisualTokens.LabelText;

    public static Color Cursor => VisualTokens.PlotCursor;

    public static Color Marker => VisualTokens.PlotMarker;

    public static Color StatusText => VisualTokens.LabelText;

    public static Color FailureText => VisualTokens.Error;

    public static StyleBox ReadoutBox { get; } = new StyleBoxFlat
    {
        BgColor = VisualTokens.ReadoutBackground,
        BorderColor = VisualTokens.PlotAxis,
        BorderWidthLeft = (int)WaveformMetrics.AxisWidth,
        BorderWidthRight = (int)WaveformMetrics.AxisWidth,
        BorderWidthTop = (int)WaveformMetrics.AxisWidth,
        BorderWidthBottom = (int)WaveformMetrics.AxisWidth,
        CornerRadiusTopLeft = (int)WaveformMetrics.ReadoutCornerRadius,
        CornerRadiusTopRight = (int)WaveformMetrics.ReadoutCornerRadius,
        CornerRadiusBottomLeft = (int)WaveformMetrics.ReadoutCornerRadius,
        CornerRadiusBottomRight = (int)WaveformMetrics.ReadoutCornerRadius,
    };

    public static Color TraceColor(int index) => VisualTokens.TraceColor(index);

    public static Color Faded(Color color) => new(color, WaveformMetrics.StaleOpacity);

    public static void ApplySplit(SplitContainer split)
    {
        ArgumentNullException.ThrowIfNull(split);

        split.AddThemeConstantOverride(SeparationConstant, WaveformMetrics.SplitterWidth);
        split.AddThemeStyleboxOverride(
            SplitBarStylebox,
            new StyleBoxFlat { BgColor = VisualTokens.SplitterBackground });
    }

    public static void ApplyPanel(Control panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.CustomMinimumSize = new Vector2(0, WaveformMetrics.MinimumPanelHeight);
        panel.ClipContents = true;
    }

    public static int HeightOf(int height) =>
        Math.Max(height, WaveformMetrics.MinimumPanelHeight);

    public static int SplitOffsetFor(int height) => -HeightOf(height);

    public static Rect2 PlotArea(Vector2 size, bool hasVoltage, bool hasCurrent)
    {
        var left = hasVoltage ? WaveformMetrics.LeftGutter : WaveformMetrics.LabelGap;
        var right = hasCurrent ? WaveformMetrics.RightGutter : WaveformMetrics.LabelGap;
        var width = Math.Max(size.X - left - right, 1);
        var height = Math.Max(
            size.Y - WaveformMetrics.TopGutter - WaveformMetrics.BottomGutter, 1);

        return new Rect2(new Vector2(left, WaveformMetrics.TopGutter), new Vector2(width, height));
    }

    public static int TimeTicksFor(float width) => TicksFor(
        width, WaveformMetrics.MinimumTickSpacing, WaveformMetrics.MaximumTimeTicks);

    public static int ValueTicksFor(float height) => TicksFor(
        height, WaveformMetrics.MinimumValueTickSpacing, WaveformMetrics.MaximumValueTicks);

    private static int TicksFor(float span, float spacing, int maximum) =>
        Math.Clamp((int)(span / spacing) + 1, 2, maximum);
}
