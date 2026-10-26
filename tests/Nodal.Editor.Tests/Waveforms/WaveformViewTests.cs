using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class WaveformViewTests
{
    private const int Width = 200;

    private static readonly Schematic Circuit = TransientRuns.Rc();

    private static TransientResult Result { get; } =
        TransientRuns.Run(Circuit, new TransientOptions(1e-6) { StartFromZero = true });

    private static WaveformView View()
    {
        var plot = WaveformPlot.Of(
            Result,
            ConnectivityAnalyzer.Analyze(Circuit, BaseContent.Catalog),
            [
                Probe.At(new PinRef(Circuit.IdOf("C1"), "1")),
                Probe.Through(Circuit.IdOf("R1")),
            ]);

        return new WaveformView(plot);
    }

    [Fact]
    public void ANewViewShowsEverything()
    {
        var view = View();

        Assert.Equal(view.Plot.TimeLimits, view.Time);
        Assert.Equal(view.Plot.VoltageLimits, view.Volts);
        Assert.Equal(view.Plot.CurrentLimits, view.Amperes);
        Assert.Null(view.CursorTime);
    }

    [Fact]
    public void FittingUndoesEveryZoomAndPan()
    {
        var view = View();

        view.ZoomTime(8, 0.5);
        view.PanTime(0.3);
        view.ZoomValues(4);
        view.Fit();

        Assert.Equal(view.Plot.TimeLimits, view.Time);
        Assert.Equal(view.Plot.VoltageLimits, view.Volts);
        Assert.Equal(view.Plot.CurrentLimits, view.Amperes);
    }

    [Fact]
    public void ZoomingTimeNarrowsTheWindowWithinTheRun()
    {
        var view = View();
        var before = view.Time.Span;

        view.ZoomTime(2, 0.5);

        Assert.Equal(before / 2, view.Time.Span, 15);
        Assert.True(view.Plot.TimeLimits.Contains(view.Time.Min));
        Assert.True(view.Plot.TimeLimits.Contains(view.Time.Max));
    }

    [Fact]
    public void ZoomingTheVerticalAxesLeavesTimeAlone()
    {
        var view = View();
        var time = view.Time;
        var volts = view.Volts.Span;
        var amperes = view.Amperes.Span;

        view.ZoomValues(2);

        Assert.Equal(time, view.Time);
        Assert.Equal(volts / 2, view.Volts.Span, 15);
        Assert.Equal(amperes / 2, view.Amperes.Span, 15);
        Assert.Equal(view.Volts, view.WindowOf(TraceAxis.Volts));
        Assert.Equal(view.Amperes, view.WindowOf(TraceAxis.Amperes));
    }

    [Fact]
    public void PanningStaysInsideTheRun()
    {
        var view = View();

        view.ZoomTime(4, 0.5);

        for (var step = 0; step < 20; step++)
        {
            view.PanTime(0.5);
        }

        Assert.Equal(view.Plot.TimeLimits.Max, view.Time.Max, 15);
    }

    [Fact]
    public void TheCursorFollowsTheFractionAcrossTheWindow()
    {
        var view = View();

        view.MoveCursorTo(0.5);

        Assert.Equal(view.Time.ValueAt(0.5), view.CursorTime);

        view.MoveCursorTo(null);

        Assert.Null(view.CursorTime);
    }

    [Fact]
    public void TheCursorIsClampedToThePlot()
    {
        var view = View();

        view.MoveCursorTo(-1);

        Assert.Equal(view.Plot.TimeLimits.Min, view.CursorTime);

        view.MoveCursorTo(2);

        Assert.Equal(view.Plot.TimeLimits.Max, view.CursorTime);
    }

    [Fact]
    public void TheReadoutIsEmptyUntilTheCursorMoves()
    {
        var view = View();

        Assert.Empty(view.Readout());

        view.MoveCursorTo(0.5);

        Assert.Equal(view.Plot.Traces.Length + 1, view.Readout().Length);
    }

    [Fact]
    public void TheAxesAreTickedInTheirOwnUnits()
    {
        var view = View();

        Assert.Contains("s", view.TimeTicks(6)[^1].Label, StringComparison.Ordinal);
        Assert.Contains("V", view.TicksOf(TraceAxis.Volts, 6)[^1].Label, StringComparison.Ordinal);
        Assert.Contains(
            "A", view.TicksOf(TraceAxis.Amperes, 6)[^1].Label, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePointsOfATraceFollowTheWindow()
    {
        var view = View();
        var trace = view.Plot.Traces[0];

        view.ZoomTime(4, 0.5);

        var points = view.PointsOf(trace, Width);

        Assert.NotEmpty(points);
        Assert.All(
            points,
            point => Assert.True(
                point.Time >= view.Plot.TimeLimits.Min
                && point.Time <= view.Plot.TimeLimits.Max));
    }

    [Fact]
    public void GoingStaleKeepsTheWindowAndTheTraces()
    {
        var view = View();

        view.ZoomTime(4, 0.5);

        var time = view.Time;
        var traces = view.Plot.Traces;

        view.MarkStale();

        Assert.True(view.Plot.IsStale);
        Assert.Equal(TransientFailureText.StaleText, view.Plot.StatusText);
        Assert.Equal(time, view.Time);
        Assert.Equal(traces, view.Plot.Traces);
    }

    [Fact]
    public void GoingStaleTwiceChangesNothing()
    {
        var view = View();

        view.MarkStale();

        var stale = view.Plot;

        view.MarkStale();

        Assert.Same(stale, view.Plot);
    }

    [Fact]
    public void AnEmptyPlotHasNoPointsAndNoCursor()
    {
        var view = new WaveformView(WaveformPlot.None);

        view.MoveCursorTo(0.5);

        Assert.Null(view.CursorTime);
        Assert.Empty(view.Readout());
    }

    [Fact]
    public void AViewNeedsAPlot() =>
        Assert.Throws<ArgumentNullException>(() => new WaveformView(null!));
}
