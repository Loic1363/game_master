using Nodal.Editor.Waveforms;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class AxisRangeTests
{
    [Fact]
    public void ARangeFitsItsValuesWithAFivePercentMargin()
    {
        var range = AxisRange.Fitting([0, 10]);

        Assert.Equal(-0.5, range.Min, 12);
        Assert.Equal(10.5, range.Max, 12);
    }

    [Fact]
    public void TheMarginIsTakenFromTheSpanOnBothSides()
    {
        var range = AxisRange.Fitting([-4, -1, 2]);

        Assert.Equal(-4.3, range.Min, 12);
        Assert.Equal(2.3, range.Max, 12);
    }

    [Fact]
    public void ARangeCrossingZeroKeepsBothSides()
    {
        var range = AxisRange.Fitting([-5, 5]);

        Assert.True(range.Min < -5);
        Assert.True(range.Max > 5);
        Assert.Equal(0, range.Centre, 12);
    }

    [Fact]
    public void AFlatTraceGetsASymmetricRangeAroundItsValue()
    {
        var range = AxisRange.Fitting([5, 5, 5]);

        Assert.Equal(0, range.Min, 12);
        Assert.Equal(10, range.Max, 12);
        Assert.Equal(5, range.Centre, 12);
    }

    [Fact]
    public void AFlatNegativeTraceGetsASymmetricRangeAroundItsValue()
    {
        var range = AxisRange.Fitting([-2e-3]);

        Assert.Equal(-4e-3, range.Min, 12);
        Assert.Equal(0, range.Max, 12);
    }

    [Fact]
    public void AFlatTraceAtZeroGetsTheUnitRange()
    {
        var range = AxisRange.Fitting([0, 0]);

        Assert.Equal(-1, range.Min, 12);
        Assert.Equal(1, range.Max, 12);
    }

    [Fact]
    public void NoValuesGiveTheUnitRangeAroundZero()
    {
        var range = AxisRange.Fitting([]);

        Assert.Equal(-1, range.Min, 12);
        Assert.Equal(1, range.Max, 12);
    }

    [Fact]
    public void NonFiniteValuesAreIgnored()
    {
        var range = AxisRange.Fitting([double.NaN, 1, double.PositiveInfinity, 3]);

        Assert.Equal(0.9, range.Min, 12);
        Assert.Equal(3.1, range.Max, 12);
    }

    [Fact]
    public void FractionsAndValuesAreInverses()
    {
        var range = new AxisRange(-1, 3);

        Assert.Equal(0, range.FractionOf(-1), 12);
        Assert.Equal(0.5, range.FractionOf(1), 12);
        Assert.Equal(1, range.FractionOf(3), 12);
        Assert.Equal(-1, range.ValueAt(0), 12);
        Assert.Equal(1, range.ValueAt(0.5), 12);
        Assert.Equal(3, range.ValueAt(1), 12);
    }

    [Fact]
    public void AFlatRangeReportsTheMiddleForEveryValue()
    {
        var range = new AxisRange(2, 2);

        Assert.True(range.IsFlat);
        Assert.Equal(0.5, range.FractionOf(2), 12);
        Assert.Equal(0.5, range.FractionOf(100), 12);
    }

    [Fact]
    public void ZoomingKeepsTheCentre()
    {
        var zoomed = new AxisRange(0, 10).ZoomedBy(2);

        Assert.Equal(2.5, zoomed.Min, 12);
        Assert.Equal(7.5, zoomed.Max, 12);
        Assert.Equal(5, zoomed.Centre, 12);
    }

    [Fact]
    public void ZoomingOutWidensAroundTheCentre()
    {
        var zoomed = new AxisRange(0, 10).ZoomedBy(0.5);

        Assert.Equal(-5, zoomed.Min, 12);
        Assert.Equal(15, zoomed.Max, 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void AnImpossibleZoomFactorChangesNothing(double factor)
    {
        var range = new AxisRange(0, 10);

        Assert.Equal(range, range.ZoomedBy(factor));
    }

    [Fact]
    public void RangesUniteAndClampAndContain()
    {
        var range = new AxisRange(0, 4).Union(new AxisRange(-2, 1));

        Assert.Equal(new AxisRange(-2, 4), range);
        Assert.Equal(-2, range.Clamp(-10), 12);
        Assert.Equal(4, range.Clamp(10), 12);
        Assert.Equal(1, range.Clamp(1), 12);
        Assert.True(range.Contains(0));
        Assert.False(range.Contains(5));
    }

    [Fact]
    public void MovingShiftsBothEnds()
    {
        var moved = new AxisRange(0, 2).MovedBy(3);

        Assert.Equal(new AxisRange(3, 5), moved);
    }
}
