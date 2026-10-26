namespace Nodal.Editor.Waveforms;

public static class PlotZoom
{
    public const double SmallestVisibleFraction = 1e-6;

    public static AxisRange TimeZoomedAt(
        AxisRange window, AxisRange limits, double factor, double fraction)
    {
        if (!double.IsFinite(factor) || factor <= 0 || limits.IsFlat)
        {
            return window;
        }

        var smallest = limits.Span * SmallestVisibleFraction;
        var span = Math.Clamp(window.Span / factor, smallest, limits.Span);
        var anchor = window.ValueAt(Math.Clamp(fraction, 0, 1));
        var minimum = anchor - (Math.Clamp(fraction, 0, 1) * span);

        return Inside(new AxisRange(minimum, minimum + span), limits);
    }

    public static AxisRange TimePannedBy(AxisRange window, AxisRange limits, double fraction)
    {
        if (!double.IsFinite(fraction) || limits.IsFlat)
        {
            return window;
        }

        return Inside(window.MovedBy(fraction * window.Span), limits);
    }

    public static AxisRange ValuesZoomedBy(AxisRange window, double factor)
    {
        var zoomed = window.ZoomedBy(factor);

        return double.IsFinite(zoomed.Span) && zoomed.Span > 0 ? zoomed : window;
    }

    private static AxisRange Inside(AxisRange window, AxisRange limits)
    {
        if (window.Span >= limits.Span)
        {
            return limits;
        }

        if (window.Min < limits.Min)
        {
            return new AxisRange(limits.Min, limits.Min + window.Span);
        }

        if (window.Max > limits.Max)
        {
            return new AxisRange(limits.Max - window.Span, limits.Max);
        }

        return window;
    }
}
