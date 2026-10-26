using System.Collections.Immutable;
using Nodal.Editor.Waveforms;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class TraceDecimationTests
{
    private const int Width = 100;

    private static ImmutableArray<double> Times(int count) =>
        [.. Enumerable.Range(0, count).Select(index => (double)index)];

    private static ImmutableArray<double> Flat(int count, double value) =>
        [.. Enumerable.Repeat(value, count)];

    private static AxisRange Window(int count) => new(0, count - 1);

    [Fact]
    public void AShortTraceIsDrawnPointByPoint()
    {
        var times = Times(50);
        var values = Times(50);
        var points = TraceDecimation.Of(times, values, Window(50), Width);

        Assert.Equal(50, points.Length);
        Assert.Equal(new PlotPoint(0, 0), points[0]);
        Assert.Equal(new PlotPoint(49, 49), points[^1]);
    }

    [Fact]
    public void ATraceWithTwicePerPixelIsStillDrawnPointByPoint()
    {
        var count = Width * TraceDecimation.PointsPerPixelBeforeDecimating;
        var points = TraceDecimation.Of(Times(count), Times(count), Window(count), Width);

        Assert.Equal(count, points.Length);
    }

    [Fact]
    public void ALongTraceIsReducedButBounded()
    {
        var count = 100_000;
        var points = TraceDecimation.Of(
            Times(count), Flat(count, 1), Window(count), Width);

        Assert.True(points.Length <= 4 * Width, $"{points.Length} points");
        Assert.NotEmpty(points);
    }

    [Fact]
    public void ASpikeSurvivesDecimation()
    {
        var count = 10_000;
        var values = Flat(count, 0).SetItem(5_000, 42);
        var points = TraceDecimation.Of(Times(count), values, Window(count), Width);

        Assert.Contains(points, point => point.Value == 42);
        Assert.Contains(points, point => point.Time == 5_000);
    }

    [Fact]
    public void ADipAndASpikeInTheSameColumnBothSurvive()
    {
        var count = 10_000;
        var values = Flat(count, 0).SetItem(5_000, 42).SetItem(5_010, -42);
        var points = TraceDecimation.Of(Times(count), values, Window(count), Width);

        Assert.Contains(points, point => point.Value == 42);
        Assert.Contains(points, point => point.Value == -42);
    }

    [Fact]
    public void TheMinimumAndMaximumKeepTheirTimeOrder()
    {
        var count = 10_000;
        var values = Flat(count, 0).SetItem(5_010, 42).SetItem(5_000, -42);
        var points = TraceDecimation.Of(Times(count), values, Window(count), Width);

        var lowest = points.First(point => point.Value == -42);
        var highest = points.First(point => point.Value == 42);

        Assert.True(points.IndexOf(lowest) < points.IndexOf(highest));
    }

    [Fact]
    public void EveryPointRisesInTime()
    {
        var count = 10_000;
        var values = Times(count);
        var points = TraceDecimation.Of(Times(count), values, Window(count), Width);

        for (var index = 1; index < points.Length; index++)
        {
            Assert.True(points[index].Time >= points[index - 1].Time);
        }
    }

    [Fact]
    public void OnlyTheWindowIsDrawn()
    {
        var count = 1_000;
        var points = TraceDecimation.Of(
            Times(count), Times(count), new AxisRange(400, 500), Width);

        Assert.All(points, point => Assert.True(point.Time >= 399 && point.Time <= 501));
        Assert.Contains(points, point => point.Time <= 400);
        Assert.Contains(points, point => point.Time >= 500);
    }

    [Fact]
    public void AWindowBeyondTheTraceDrawsNothingOrTheNearestEdge()
    {
        var count = 100;
        var after = TraceDecimation.Of(
            Times(count), Times(count), new AxisRange(1_000, 2_000), Width);

        Assert.All(after, point => Assert.Equal(99, point.Time));
    }

    [Fact]
    public void AnEmptyTraceDrawsNothing()
    {
        Assert.Empty(TraceDecimation.Of([], [], new AxisRange(0, 1), Width));
        Assert.Empty(TraceDecimation.Of(Times(10), [], new AxisRange(0, 1), Width));
    }

    [Fact]
    public void AZeroWidthPlotStillDrawsSomething()
    {
        var count = 1_000;
        var points = TraceDecimation.Of(Times(count), Times(count), Window(count), 0);

        Assert.NotEmpty(points);
        Assert.True(points.Length <= 4);
    }

    [Fact]
    public void TheFirstAndLastValueOfEachColumnAreKept()
    {
        var count = 400;
        var values = Times(count);
        var points = TraceDecimation.Of(Times(count), values, Window(count), 10);

        Assert.Equal(0, points[0].Value);
        Assert.Equal(399, points[^1].Value);
    }
}
