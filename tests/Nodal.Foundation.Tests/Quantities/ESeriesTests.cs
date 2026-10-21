using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class ESeriesTests
{
    [Theory]
    [InlineData(StandardSeries.E6, 1.0, true)]
    [InlineData(StandardSeries.E6, 4700.0, true)]
    [InlineData(StandardSeries.E6, 4.7e-9, true)]
    [InlineData(StandardSeries.E6, 6.8e6, true)]
    [InlineData(StandardSeries.E6, 1.5e12, true)]
    [InlineData(StandardSeries.E6, 1e-15, true)]
    [InlineData(StandardSeries.E6, 5000.0, false)]
    [InlineData(StandardSeries.E6, 8200.0, false)]
    [InlineData(StandardSeries.E6, 0.91, false)]
    [InlineData(StandardSeries.E12, 8200.0, true)]
    [InlineData(StandardSeries.E12, 3.9e-6, true)]
    [InlineData(StandardSeries.E12, 9100.0, false)]
    [InlineData(StandardSeries.E12, 1100.0, false)]
    [InlineData(StandardSeries.E24, 9100.0, true)]
    [InlineData(StandardSeries.E24, 0.91, true)]
    [InlineData(StandardSeries.E24, 1.1e-15, true)]
    [InlineData(StandardSeries.E24, 7.5e9, true)]
    [InlineData(StandardSeries.E24, 5000.0, false)]
    [InlineData(StandardSeries.E24, 10000.0, true)]
    public void ContainsRecognisesSeriesValuesInEveryDecade(StandardSeries series, double value, bool expected)
    {
        Assert.Equal(expected, ESeries.Contains(series, value));
    }

    [Theory]
    [InlineData(StandardSeries.E6, 5000.0, 4700.0)]
    [InlineData(StandardSeries.E6, 6000.0, 6800.0)]
    [InlineData(StandardSeries.E6, 1000.0, 1000.0)]
    [InlineData(StandardSeries.E6, 9.6, 10.0)]
    [InlineData(StandardSeries.E12, 1000.0, 1000.0)]
    [InlineData(StandardSeries.E12, 5000.0, 4700.0)]
    [InlineData(StandardSeries.E24, 5000.0, 5100.0)]
    [InlineData(StandardSeries.E24, 9.6, 10.0)]
    [InlineData(StandardSeries.E24, 0.95, 0.91)]
    [InlineData(StandardSeries.E24, 3.3e-9, 3.3e-9)]
    public void NearestPicksTheClosestValueOnALogarithmicScale(
        StandardSeries series, double value, double expected)
    {
        Approximately.Equal(expected, ESeries.Nearest(series, value));
    }

    [Theory]
    [InlineData(StandardSeries.E6, 4.7, 6.8)]
    [InlineData(StandardSeries.E12, 4.7, 5.6)]
    [InlineData(StandardSeries.E24, 1.0, 1.1)]
    public void NearestBreaksTiesTowardsTheHigherValue(StandardSeries series, double lower, double higher)
    {
        var midpoint = Math.Sqrt(lower * higher);

        Approximately.Equal(higher, ESeries.Nearest(series, midpoint));
    }

    [Theory]
    [InlineData(StandardSeries.E24, 9.1, 10.0)]
    [InlineData(StandardSeries.E24, 1.0, 1.1)]
    [InlineData(StandardSeries.E24, 0.91, 1.0)]
    [InlineData(StandardSeries.E24, 9100.0, 10000.0)]
    [InlineData(StandardSeries.E24, 5.0, 5.1)]
    [InlineData(StandardSeries.E12, 8200.0, 10000.0)]
    [InlineData(StandardSeries.E6, 6.8, 10.0)]
    [InlineData(StandardSeries.E6, 1.0, 1.5)]
    [InlineData(StandardSeries.E6, 6.8e-15, 1e-14)]
    public void NextStepsUpAcrossDecades(StandardSeries series, double value, double expected)
    {
        Approximately.Equal(expected, ESeries.Next(series, value));
    }

    [Theory]
    [InlineData(StandardSeries.E24, 1.0, 0.91)]
    [InlineData(StandardSeries.E24, 10.0, 9.1)]
    [InlineData(StandardSeries.E24, 10000.0, 9100.0)]
    [InlineData(StandardSeries.E24, 5.0, 4.7)]
    [InlineData(StandardSeries.E12, 1.0, 0.82)]
    [InlineData(StandardSeries.E6, 1.0, 0.68)]
    [InlineData(StandardSeries.E6, 1.5, 1.0)]
    [InlineData(StandardSeries.E6, 1e-14, 6.8e-15)]
    public void PreviousStepsDownAcrossDecades(StandardSeries series, double value, double expected)
    {
        Approximately.Equal(expected, ESeries.Previous(series, value));
    }

    [Fact]
    public void NextAndPreviousAreInverseOnSeriesValues()
    {
        var value = 1.0;

        for (var step = 0; step < 50; step++)
        {
            var stepped = ESeries.Next(StandardSeries.E24, value);

            Approximately.Equal(value, ESeries.Previous(StandardSeries.E24, stepped));

            value = stepped;
        }

        Approximately.Equal(1.2e2, value);
    }

    [Fact]
    public void ValuesInRangeIsInclusiveAndAscending()
    {
        Approximately.EqualSequence(
            [1.0, 1.5, 2.2, 3.3, 4.7, 6.8, 10.0],
            ESeries.ValuesInRange(StandardSeries.E6, 1.0, 10.0));

        Approximately.EqualSequence(
            [1000.0, 1500.0, 2200.0, 3300.0, 4700.0],
            ESeries.ValuesInRange(StandardSeries.E6, 1000.0, 4700.0));

        Approximately.EqualSequence(
            [1.0, 1.2],
            ESeries.ValuesInRange(StandardSeries.E12, 0.9, 1.3));

        Approximately.EqualSequence(
            [0.91, 1.0, 1.1, 1.2, 1.3],
            ESeries.ValuesInRange(StandardSeries.E24, 0.9, 1.3));

        Approximately.EqualSequence(
            [4.7e-9],
            ESeries.ValuesInRange(StandardSeries.E6, 4e-9, 5e-9));
    }

    [Fact]
    public void ValuesInRangeIsEmptyWhenTheRangeIsInverted()
    {
        Assert.Empty(ESeries.ValuesInRange(StandardSeries.E24, 10.0, 1.0));
    }

    [Fact]
    public void ValuesInRangeSpansManyDecades()
    {
        var values = ESeries.ValuesInRange(StandardSeries.E6, 1.0, 1e6);

        Assert.Equal(37, values.Count);
        Approximately.Equal(1.0, values[0]);
        Approximately.Equal(1e6, values[^1]);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonPositiveValuesAreRejectedByEveryOperation(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ESeries.Contains(StandardSeries.E24, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => ESeries.Nearest(StandardSeries.E24, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => ESeries.Next(StandardSeries.E24, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => ESeries.Previous(StandardSeries.E24, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => ESeries.ValuesInRange(StandardSeries.E24, value, 10.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ESeries.ValuesInRange(StandardSeries.E24, 1.0, value));
    }

    [Theory]
    [InlineData(StandardSeries.E6, 6)]
    [InlineData(StandardSeries.E12, 12)]
    [InlineData(StandardSeries.E24, 24)]
    public void EachSeriesHasTheExpectedNumberOfValuesPerDecade(StandardSeries series, int expectedCount)
    {
        Assert.Equal(expectedCount, ESeries.ValuesInRange(series, 1.0, 9.99).Count);
    }
}
