using Nodal.Editor.Waveforms;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class AxisTicksTests
{
    private const int MaxTicks = 6;

    [Fact]
    public void TheTimeAxisOfAFiveHundredMicrosecondRunIsLabelledWithSiPrefixes()
    {
        var ticks = AxisTicks.Of(new AxisRange(0, 500e-6), MaxTicks, Unit.Second);

        Assert.Equal("0 s", ticks[0].Label);
        Assert.Equal("100 µs", ticks[1].Label);
        Assert.Equal("200 µs", ticks[2].Label);
        Assert.Equal("500 µs", ticks[^1].Label);
    }

    [Fact]
    public void AVoltAxisIsLabelledInVolts()
    {
        var ticks = AxisTicks.Of(new AxisRange(0, 5), 6, Unit.Volt);

        Assert.Equal("0 V", ticks[0].Label);
        Assert.Equal("1 V", ticks[1].Label);
        Assert.Equal("2 V", ticks[2].Label);
        Assert.Equal("5 V", ticks[^1].Label);
    }

    [Fact]
    public void AMilliampereAxisIsLabelledWithThePrefix()
    {
        var ticks = AxisTicks.Of(new AxisRange(0, 5e-3), 2, Unit.Ampere);

        Assert.Equal("0 A", ticks[0].Label);
        Assert.Equal("5 mA", ticks[^1].Label);
    }

    [Theory]
    [InlineData(1e-9, "ns")]
    [InlineData(1e-6, "µs")]
    [InlineData(1e-3, "ms")]
    [InlineData(1, "s")]
    public void EveryTimeDecadeFromNanosecondsToSecondsIsLabelled(double stop, string expected)
    {
        var ticks = AxisTicks.Of(new AxisRange(0, stop), MaxTicks, Unit.Second);

        Assert.True(ticks.Length >= 2);
        Assert.Contains(expected, ticks[^1].Label, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1e-6, "µV")]
    [InlineData(1e-3, "mV")]
    [InlineData(1, "V")]
    [InlineData(1e3, "kV")]
    public void EveryVoltageDecadeFromMicrovoltsToKilovoltsIsLabelled(
        double top, string expected)
    {
        var ticks = AxisTicks.Of(new AxisRange(0, top), MaxTicks, Unit.Volt);

        Assert.True(ticks.Length >= 2);
        Assert.Contains(expected, ticks[^1].Label, StringComparison.Ordinal);
    }

    [Fact]
    public void ANegativeRangeIsTicked()
    {
        var ticks = AxisTicks.Of(new AxisRange(-10, -2), MaxTicks, Unit.Volt);

        Assert.All(ticks, tick => Assert.True(tick.Value <= -2));
        Assert.All(ticks, tick => Assert.True(tick.Value >= -10));
        Assert.Contains(ticks, tick => tick.Label == "-10 V");
    }

    [Fact]
    public void ARangeCrossingZeroIncludesZero()
    {
        var ticks = AxisTicks.Of(new AxisRange(-3, 4), MaxTicks, Unit.Volt);

        Assert.Contains(ticks, tick => tick.Value == 0);
        Assert.Contains(ticks, tick => tick.Label == "0 V");
    }

    [Fact]
    public void AFlatRangeGivesASingleTick()
    {
        var tick = Assert.Single(AxisTicks.Of(new AxisRange(2, 2), MaxTicks, Unit.Volt));

        Assert.Equal(2, tick.Value, 12);
        Assert.Equal("2 V", tick.Label);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void NeverMoreTicksThanAsked(int maxTicks)
    {
        var ticks = AxisTicks.Of(new AxisRange(-0.37, 12.9), maxTicks, Unit.Volt);

        Assert.True(ticks.Length <= maxTicks, $"{ticks.Length} ticks for {maxTicks}");
        Assert.NotEmpty(ticks);
    }

    [Fact]
    public void FewerThanTwoTicksAreNotAsked()
    {
        Assert.NotEmpty(AxisTicks.Of(new AxisRange(0, 1), 0, Unit.Volt));
        Assert.NotEmpty(AxisTicks.Of(new AxisRange(0, 1), -5, Unit.Volt));
    }

    [Theory]
    [InlineData(1, 6, 0.2)]
    [InlineData(10, 6, 2)]
    [InlineData(100, 6, 20)]
    [InlineData(3, 6, 1)]
    [InlineData(7, 6, 2)]
    [InlineData(23, 6, 5)]
    public void TheStepFollowsTheOneTwoFiveSequence(double span, int maxTicks, double expected)
    {
        Assert.Equal(expected, AxisTicks.StepFor(span, maxTicks), 12);
    }

    [Fact]
    public void EveryStepIsAOneTwoOrFiveTimesAPowerOfTen()
    {
        for (var exponent = -12; exponent <= 6; exponent++)
        {
            foreach (var factor in new[] { 1.0, 1.7, 3.3, 6.5, 9.9 })
            {
                var step = AxisTicks.StepFor(factor * Math.Pow(10, exponent), 6);
                var decade = Math.Pow(10, Math.Floor(Math.Log10(step)));
                var multiple = step / decade;

                Assert.True(
                    Math.Abs(multiple - 1) < 1e-9
                    || Math.Abs(multiple - 2) < 1e-9
                    || Math.Abs(multiple - 5) < 1e-9,
                    $"The step {step} is not a 1-2-5 step.");
            }
        }
    }

    [Fact]
    public void TicksRiseAndStayInsideTheRange()
    {
        var range = new AxisRange(-0.0037, 0.129);
        var ticks = AxisTicks.Of(range, 8, Unit.Volt);

        for (var index = 1; index < ticks.Length; index++)
        {
            Assert.True(ticks[index].Value > ticks[index - 1].Value);
        }

        Assert.All(ticks, tick => Assert.True(range.Contains(tick.Value)));
    }
}
