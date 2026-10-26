using System.Collections.Immutable;
using Godot;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Symbols;
using Nodal.Editor.Waveforms;
using Nodal.Game.Style;

namespace Nodal.Game.Rendering;

public partial class WaveformPanel : Control
{
    private readonly List<LegendEntry> _legend = [];

    private WaveformView? _view;
    private Font? _font;
    private double? _cursorTime;
    private bool _panning;

    public event Action<double?>? CursorTimeChanged;

    public event Action<Probe>? ProbeRemoved;

    public bool HasPlot => _view is not null;

    private Font Font => _font ??= GetThemeDefaultFont();

    public override void _Ready()
    {
        _font = GetThemeDefaultFont();

        WaveformStyle.ApplyPanel(this);

        MouseExited += OnMouseExited;
    }

    public void ShowPlot(WaveformPlot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);

        _view = new WaveformView(plot);

        ReportCursor(null);
        QueueRedraw();
    }

    public void MarkStale()
    {
        if (_view is not { } view || view.Plot.IsStale)
        {
            return;
        }

        view.MarkStale();

        ReportCursor(null);
        QueueRedraw();
    }

    public void Clear()
    {
        _view = null;
        _legend.Clear();

        ReportCursor(null);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), WaveformStyle.Background);

        _legend.Clear();

        if (_view is not { } view)
        {
            return;
        }

        var area = AreaOf(view);

        DrawGrid(view, area);
        DrawTraces(view, area);
        DrawFailureMarker(view, area);
        DrawAxes(view, area);
        DrawLegend(view, area);
        DrawCursor(view, area);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_view is not { } view)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton button:
                HandleButton(view, button);
                break;

            case InputEventMouseMotion motion:
                HandleMotion(view, motion);
                break;
        }
    }

    private Rect2 AreaOf(WaveformView view) =>
        WaveformStyle.PlotArea(Size, view.Plot.HasVoltage, view.Plot.HasCurrent);

    private void OnMouseExited()
    {
        _panning = false;

        MoveCursor(null);
    }

    private void HandleButton(WaveformView view, InputEventMouseButton button)
    {
        switch (button.ButtonIndex)
        {
            case MouseButton.WheelUp when button.Pressed:
                Zoom(view, WaveformMetrics.ZoomStep, button);
                break;

            case MouseButton.WheelDown when button.Pressed:
                Zoom(view, 1f / WaveformMetrics.ZoomStep, button);
                break;

            case MouseButton.Left when button.Pressed:
                PressLeft(view, button);
                break;

            case MouseButton.Left:
                _panning = false;
                break;
        }
    }

    private void PressLeft(WaveformView view, InputEventMouseButton button)
    {
        if (ProbeAt(button.Position) is { } probe)
        {
            ProbeRemoved?.Invoke(probe);

            return;
        }

        if (button.DoubleClick)
        {
            view.Fit();
            MoveCursor(button.Position);
            QueueRedraw();

            return;
        }

        _panning = true;
    }

    private void HandleMotion(WaveformView view, InputEventMouseMotion motion)
    {
        if (_panning)
        {
            view.PanTime(-motion.Relative.X / AreaOf(view).Size.X);
        }

        MoveCursor(motion.Position);
        QueueRedraw();
    }

    private void Zoom(WaveformView view, float factor, InputEventMouseButton button)
    {
        if (button.CtrlPressed)
        {
            view.ZoomValues(factor);
        }
        else
        {
            view.ZoomTime(factor, FractionAt(view, button.Position));
        }

        MoveCursor(button.Position);
        QueueRedraw();
    }

    private void MoveCursor(Vector2? position)
    {
        if (_view is not { } view)
        {
            return;
        }

        var area = AreaOf(view);

        view.MoveCursorTo(position is { } at && area.HasPoint(at) ? FractionAt(view, at) : null);

        ReportCursor(view.CursorTime);
    }

    private void ReportCursor(double? time)
    {
        if (Nullable.Equals(_cursorTime, time))
        {
            return;
        }

        _cursorTime = time;

        CursorTimeChanged?.Invoke(time);
        QueueRedraw();
    }

    private double FractionAt(WaveformView view, Vector2 position)
    {
        var area = AreaOf(view);

        return Math.Clamp((position.X - area.Position.X) / area.Size.X, 0, 1);
    }

    private Probe? ProbeAt(Vector2 position)
    {
        foreach (var entry in _legend)
        {
            if (entry.Bounds.HasPoint(position))
            {
                return entry.Probe;
            }
        }

        return null;
    }

    private static float XOf(WaveformView view, Rect2 area, double time) =>
        area.Position.X + ((float)view.Time.FractionOf(time) * area.Size.X);

    private static float YOf(Rect2 area, AxisRange window, double value) => Math.Clamp(
        area.End.Y - ((float)window.FractionOf(value) * area.Size.Y),
        area.Position.Y,
        area.End.Y);

    private static TraceAxis? PrimaryAxisOf(WaveformPlot plot) => plot.HasVoltage
        ? TraceAxis.Volts
        : plot.HasCurrent ? TraceAxis.Amperes : null;

    private static Color Tinted(WaveformView view, Color color) =>
        view.Plot.IsStale ? WaveformStyle.Faded(color) : color;

    private void DrawGrid(WaveformView view, Rect2 area)
    {
        foreach (var tick in view.TimeTicks(WaveformStyle.TimeTicksFor(area.Size.X)))
        {
            var x = XOf(view, area, tick.Value);

            DrawLine(
                new Vector2(x, area.Position.Y),
                new Vector2(x, area.End.Y),
                WaveformStyle.Grid,
                WaveformMetrics.GridWidth);
        }

        if (PrimaryAxisOf(view.Plot) is not { } axis)
        {
            return;
        }

        foreach (var tick in view.TicksOf(axis, WaveformStyle.ValueTicksFor(area.Size.Y)))
        {
            var y = YOf(area, view.WindowOf(axis), tick.Value);

            DrawLine(
                new Vector2(area.Position.X, y),
                new Vector2(area.End.X, y),
                WaveformStyle.Grid,
                WaveformMetrics.GridWidth);
        }
    }

    private void DrawTraces(WaveformView view, Rect2 area)
    {
        foreach (var trace in view.Plot.Traces)
        {
            var points = view.PointsOf(trace, (int)area.Size.X);

            if (points.Length < 2)
            {
                continue;
            }

            DrawPolyline(
                Mapped(view, area, view.WindowOf(trace.Axis), points),
                Tinted(view, WaveformStyle.TraceColor(trace.ColorIndex)),
                WaveformMetrics.TraceWidth);
        }
    }

    private static Vector2[] Mapped(
        WaveformView view, Rect2 area, AxisRange window, ImmutableArray<PlotPoint> points)
    {
        var mapped = new Vector2[points.Length];

        for (var index = 0; index < points.Length; index++)
        {
            mapped[index] = new Vector2(
                XOf(view, area, points[index].Time), YOf(area, window, points[index].Value));
        }

        return mapped;
    }

    private void DrawFailureMarker(WaveformView view, Rect2 area)
    {
        if (!view.Plot.IsPartial
            || view.Plot.FailureTime is not { } time
            || !view.Time.Contains(time))
        {
            return;
        }

        var x = XOf(view, area, time);
        var color = Tinted(view, WaveformStyle.Marker);

        DrawLine(
            new Vector2(x, area.Position.Y),
            new Vector2(x, area.End.Y),
            color,
            WaveformMetrics.MarkerWidth);

        DrawRect(
            new Rect2(
                new Vector2(x - WaveformMetrics.MarkerHalfWidth, area.Position.Y),
                new Vector2(
                    WaveformMetrics.MarkerHalfWidth * 2f, WaveformMetrics.MarkerWidth * 2f)),
            color);
    }

    private void DrawAxes(WaveformView view, Rect2 area)
    {
        DrawLine(
            new Vector2(area.Position.X, area.End.Y),
            new Vector2(area.End.X, area.End.Y),
            WaveformStyle.Axis,
            WaveformMetrics.AxisWidth);

        foreach (var tick in view.TimeTicks(WaveformStyle.TimeTicksFor(area.Size.X)))
        {
            var x = XOf(view, area, tick.Value);

            DrawLine(
                new Vector2(x, area.End.Y),
                new Vector2(x, area.End.Y + WaveformMetrics.TickLength),
                WaveformStyle.Axis,
                WaveformMetrics.AxisWidth);

            CanvasText.DrawCentered(
                this,
                Font,
                new Vector2(
                    x,
                    area.End.Y + WaveformMetrics.TickLength + (WaveformMetrics.LabelGap * 2f)),
                tick.Label,
                WaveformMetrics.FontSize,
                WaveformStyle.AxisText);
        }

        if (view.Plot.HasVoltage)
        {
            DrawValueAxis(view, area, TraceAxis.Volts, atLeft: true);
        }

        if (view.Plot.HasCurrent)
        {
            DrawValueAxis(view, area, TraceAxis.Amperes, atLeft: false);
        }
    }

    private void DrawValueAxis(WaveformView view, Rect2 area, TraceAxis axis, bool atLeft)
    {
        var edge = atLeft ? area.Position.X : area.End.X;
        var outward = atLeft ? -WaveformMetrics.TickLength : WaveformMetrics.TickLength;
        var gap = atLeft ? -WaveformMetrics.LabelGap : WaveformMetrics.LabelGap;

        DrawLine(
            new Vector2(edge, area.Position.Y),
            new Vector2(edge, area.End.Y),
            WaveformStyle.Axis,
            WaveformMetrics.AxisWidth);

        foreach (var tick in view.TicksOf(axis, WaveformStyle.ValueTicksFor(area.Size.Y)))
        {
            var y = YOf(area, view.WindowOf(axis), tick.Value);

            DrawLine(
                new Vector2(edge, y),
                new Vector2(edge + outward, y),
                WaveformStyle.Axis,
                WaveformMetrics.AxisWidth);

            var anchor = new Vector2(edge + outward + gap, y);

            if (atLeft)
            {
                CanvasText.DrawRightAligned(
                    this,
                    Font,
                    anchor,
                    tick.Label,
                    WaveformMetrics.FontSize,
                    WaveformStyle.AxisText);
            }
            else
            {
                CanvasText.DrawAligned(
                    this,
                    Font,
                    anchor,
                    LabelAlignment.Left,
                    tick.Label,
                    WaveformMetrics.FontSize,
                    WaveformStyle.AxisText);
            }
        }
    }

    private void DrawLegend(WaveformView view, Rect2 area)
    {
        var y = WaveformMetrics.TopGutter / 2f;
        var x = area.Position.X;

        foreach (var trace in view.Plot.Traces)
        {
            var width = Font.GetStringSize(
                trace.Label, HorizontalAlignment.Left, -1, WaveformMetrics.FontSize).X;
            var textLeft = x + WaveformMetrics.LegendSwatchWidth + WaveformMetrics.LegendSwatchGap;
            var color = Tinted(view, WaveformStyle.TraceColor(trace.ColorIndex));

            DrawLine(
                new Vector2(x, y),
                new Vector2(x + WaveformMetrics.LegendSwatchWidth, y),
                color,
                WaveformMetrics.TraceWidth);

            CanvasText.DrawAligned(
                this,
                Font,
                new Vector2(textLeft, y),
                LabelAlignment.Left,
                trace.Label,
                WaveformMetrics.FontSize,
                color);

            _legend.Add(
                new LegendEntry(
                    new Rect2(
                        new Vector2(x, 0),
                        new Vector2(textLeft + width - x, WaveformMetrics.TopGutter)),
                    trace.Probe));

            x = textLeft + width + WaveformMetrics.LegendGap;
        }

        if (view.Plot.StatusText is { } status)
        {
            CanvasText.DrawAligned(
                this,
                Font,
                new Vector2(x, y),
                LabelAlignment.Left,
                status,
                WaveformMetrics.FontSize,
                view.Plot.IsStale ? WaveformStyle.StatusText : WaveformStyle.FailureText);
        }
    }

    private void DrawCursor(WaveformView view, Rect2 area)
    {
        if (view.CursorTime is not { } time)
        {
            return;
        }

        var x = XOf(view, area, time);

        DrawLine(
            new Vector2(x, area.Position.Y),
            new Vector2(x, area.End.Y),
            WaveformStyle.Cursor,
            WaveformMetrics.CursorWidth);

        DrawReadout(view, area, x);
    }

    private void DrawReadout(WaveformView view, Rect2 area, float cursorX)
    {
        var lines = view.Readout();

        if (lines.IsEmpty)
        {
            return;
        }

        var texts = lines.Select(line => $"{line.Label} {line.Text}").ToArray();
        var lineHeight = Font.GetHeight(WaveformMetrics.FontSize);
        var widest = texts.Max(
            text => Font.GetStringSize(
                text, HorizontalAlignment.Left, -1, WaveformMetrics.FontSize).X);

        var size = new Vector2(
            widest + (WaveformMetrics.ReadoutPaddingX * 2f),
            (texts.Length * lineHeight)
            + ((texts.Length - 1) * WaveformMetrics.ReadoutLineGap)
            + (WaveformMetrics.ReadoutPaddingY * 2f));

        var box = new Rect2(ReadoutOriginOf(area, cursorX, size), size);

        DrawStyleBox(WaveformStyle.ReadoutBox, box);

        var y = box.Position.Y + WaveformMetrics.ReadoutPaddingY + (lineHeight / 2f);

        for (var index = 0; index < texts.Length; index++)
        {
            CanvasText.DrawAligned(
                this,
                Font,
                new Vector2(box.Position.X + WaveformMetrics.ReadoutPaddingX, y),
                LabelAlignment.Left,
                texts[index],
                WaveformMetrics.FontSize,
                ReadoutColorOf(view, index));

            y += lineHeight + WaveformMetrics.ReadoutLineGap;
        }
    }

    private static Vector2 ReadoutOriginOf(Rect2 area, float cursorX, Vector2 size)
    {
        var left = cursorX + WaveformMetrics.ReadoutOffset;

        if (left + size.X > area.End.X)
        {
            left = cursorX - WaveformMetrics.ReadoutOffset - size.X;
        }

        return new Vector2(
            Math.Clamp(left, area.Position.X, Math.Max(area.End.X - size.X, area.Position.X)),
            Math.Clamp(
                area.Position.Y + WaveformMetrics.ReadoutOffset,
                area.Position.Y,
                Math.Max(area.End.Y - size.Y, area.Position.Y)));
    }

    private static Color ReadoutColorOf(WaveformView view, int index) =>
        index == 0 || index > view.Plot.Traces.Length
            ? WaveformStyle.StatusText
            : WaveformStyle.TraceColor(view.Plot.Traces[index - 1].ColorIndex);

    private sealed record LegendEntry(Rect2 Bounds, Probe Probe);
}
