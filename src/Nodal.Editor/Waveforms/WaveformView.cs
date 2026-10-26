using System.Collections.Immutable;
using Nodal.Foundation.Quantities;

namespace Nodal.Editor.Waveforms;

public sealed class WaveformView
{
    private WaveformPlot _plot;

    public WaveformView(WaveformPlot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);

        _plot = plot;

        Fit();
    }

    public WaveformPlot Plot => _plot;

    public AxisRange Time { get; private set; }

    public AxisRange Volts { get; private set; }

    public AxisRange Amperes { get; private set; }

    public double? CursorTime { get; private set; }

    public void Fit()
    {
        Time = _plot.TimeLimits;
        Volts = _plot.VoltageLimits;
        Amperes = _plot.CurrentLimits;
    }

    public void MarkStale() => _plot = _plot.Staled();

    public void ZoomTime(double factor, double fraction) =>
        Time = PlotZoom.TimeZoomedAt(Time, _plot.TimeLimits, factor, fraction);

    public void PanTime(double fraction) =>
        Time = PlotZoom.TimePannedBy(Time, _plot.TimeLimits, fraction);

    public void ZoomValues(double factor)
    {
        Volts = PlotZoom.ValuesZoomedBy(Volts, factor);
        Amperes = PlotZoom.ValuesZoomedBy(Amperes, factor);
    }

    public void MoveCursorTo(double? fraction)
    {
        if (fraction is not { } position || _plot.Result is not { HasValues: true } result)
        {
            CursorTime = null;

            return;
        }

        CursorTime = CursorReadout.ClampedTime(
            result, Time.ValueAt(Math.Clamp(position, 0, 1)));
    }

    public AxisRange WindowOf(TraceAxis axis) => axis == TraceAxis.Volts ? Volts : Amperes;

    public ImmutableArray<AxisTick> TimeTicks(int maxTicks) =>
        AxisTicks.Of(Time, maxTicks, Unit.Second);

    public ImmutableArray<AxisTick> TicksOf(TraceAxis axis, int maxTicks) =>
        AxisTicks.Of(
            WindowOf(axis), maxTicks, axis == TraceAxis.Volts ? Unit.Volt : Unit.Ampere);

    public ImmutableArray<PlotPoint> PointsOf(WaveformTrace trace, int widthInPixels)
    {
        ArgumentNullException.ThrowIfNull(trace);

        return _plot.Result is { } result
            ? TraceDecimation.Of(result.Times, trace.Values, Time, widthInPixels)
            : [];
    }

    public ImmutableArray<ReadoutLine> Readout() =>
        _plot.Result is { } result && CursorTime is { } time
            ? CursorReadout.Of(result, _plot.Traces, time)
            : [];
}
