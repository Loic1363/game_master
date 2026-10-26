using Nodal.Editor.Waveforms;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class PlotZoomTests
{
    private static readonly AxisRange Limits = new(0, 1e-3);

    [Fact]
    public void ZoomingInHalvesTheWindowAroundTheCursor()
    {
        var zoomed = PlotZoom.TimeZoomedAt(Limits, Limits, 2, 0.5);

        Assert.Equal(0.25e-3, zoomed.Min, 12);
        Assert.Equal(0.75e-3, zoomed.Max, 12);
    }

    [Fact]
    public void ZoomingInAtTheLeftEdgeKeepsTheLeftEdge()
    {
        var zoomed = PlotZoom.TimeZoomedAt(Limits, Limits, 2, 0);

        Assert.Equal(0, zoomed.Min, 12);
        Assert.Equal(0.5e-3, zoomed.Max, 12);
    }

    [Fact]
    public void ZoomingInAtTheRightEdgeKeepsTheRightEdge()
    {
        var zoomed = PlotZoom.TimeZoomedAt(Limits, Limits, 2, 1);

        Assert.Equal(0.5e-3, zoomed.Min, 12);
        Assert.Equal(1e-3, zoomed.Max, 12);
    }

    [Fact]
    public void TheCursorTimeStaysUnderTheCursor()
    {
        var window = new AxisRange(0.2e-3, 0.6e-3);
        var before = window.ValueAt(0.25);
        var zoomed = PlotZoom.TimeZoomedAt(window, Limits, 4, 0.25);

        Assert.Equal(before, zoomed.ValueAt(0.25), 12);
    }

    [Fact]
    public void ZoomingOutNeverLeavesTheRun()
    {
        var zoomed = PlotZoom.TimeZoomedAt(new AxisRange(0.4e-3, 0.6e-3), Limits, 0.01, 0.5);

        Assert.Equal(Limits, zoomed);
    }

    [Fact]
    public void ZoomingInStopsAtATinyWindow()
    {
        var window = Limits;

        for (var step = 0; step < 100; step++)
        {
            window = PlotZoom.TimeZoomedAt(window, Limits, 2, 0.5);
        }

        Assert.True(window.Span > 0);
        Assert.Equal(Limits.Span * PlotZoom.SmallestVisibleFraction, window.Span, 15);
        Assert.True(Limits.Contains(window.Min));
        Assert.True(Limits.Contains(window.Max));
    }

    [Fact]
    public void PanningMovesTheWindowByAFractionOfItself()
    {
        var panned = PlotZoom.TimePannedBy(new AxisRange(0.2e-3, 0.4e-3), Limits, 0.5);

        Assert.Equal(0.3e-3, panned.Min, 12);
        Assert.Equal(0.5e-3, panned.Max, 12);
    }

    [Fact]
    public void PanningStopsAtTheStartOfTheRun()
    {
        var panned = PlotZoom.TimePannedBy(new AxisRange(0.1e-3, 0.3e-3), Limits, -10);

        Assert.Equal(0, panned.Min, 12);
        Assert.Equal(0.2e-3, panned.Max, 12);
    }

    [Fact]
    public void PanningStopsAtTheEndOfTheRun()
    {
        var panned = PlotZoom.TimePannedBy(new AxisRange(0.7e-3, 0.9e-3), Limits, 10);

        Assert.Equal(0.8e-3, panned.Min, 12);
        Assert.Equal(1e-3, panned.Max, 12);
    }

    [Fact]
    public void PanningAFullWindowChangesNothing()
    {
        Assert.Equal(Limits, PlotZoom.TimePannedBy(Limits, Limits, 0.5));
    }

    [Fact]
    public void AnEmptyRunCannotBeZoomedOrPanned()
    {
        var window = new AxisRange(0, 0);

        Assert.Equal(window, PlotZoom.TimeZoomedAt(window, window, 2, 0.5));
        Assert.Equal(window, PlotZoom.TimePannedBy(window, window, 0.5));
    }

    [Fact]
    public void TheVerticalAxisZoomsAroundItsCentre()
    {
        var zoomed = PlotZoom.ValuesZoomedBy(new AxisRange(-2, 6), 2);

        Assert.Equal(0, zoomed.Min, 12);
        Assert.Equal(4, zoomed.Max, 12);
    }

    [Fact]
    public void AVerticalZoomThatWouldCollapseTheAxisIsIgnored()
    {
        var window = new AxisRange(1, 1);

        Assert.Equal(window, PlotZoom.ValuesZoomedBy(window, 2));
        Assert.Equal(
            new AxisRange(0, 4), PlotZoom.ValuesZoomedBy(new AxisRange(0, 4), double.NaN));
    }
}
