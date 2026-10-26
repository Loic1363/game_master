using Nodal.Foundation.Tests.Quantities;
using Nodal.Foundation.Waveforms;
using Xunit;

namespace Nodal.Foundation.Tests.Waveforms;

public class WaveformValueTests
{
    private const double NgspiceTolerance = 1e-7;

    private static PulseWaveform Pulse(int? cycles = null) =>
        new(0, 12, 1e-3, 1e-3, 1e-3, 2e-3, 5e-3, cycles);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1e-3, 0)]
    [InlineData(1.5e-3, 6)]
    [InlineData(2e-3, 12)]
    [InlineData(3e-3, 12)]
    [InlineData(4e-3, 12)]
    [InlineData(4.5e-3, 6)]
    [InlineData(5e-3, 0)]
    [InlineData(5.5e-3, 0)]
    public void PulseFollowsTheNgspiceRampsV1ThenRiseThenV2ThenFall(double time, double expected)
    {
        Approximately.Equal(expected, Pulse().ValueAt(time), NgspiceTolerance);
    }

    [Fact]
    public void PulseRepeatsEveryPeriodAfterTheDelay()
    {
        Approximately.Equal(6, Pulse().ValueAt(6.5e-3), NgspiceTolerance);
        Approximately.Equal(12, Pulse().ValueAt(8e-3), NgspiceTolerance);
    }

    [Fact]
    public void PulseHoldsV1AfterItsLastCycleAsNgspiceDoesWithNp()
    {
        var pulse = Pulse(cycles: 2);

        Approximately.Equal(12, pulse.ValueAt(8e-3), NgspiceTolerance);
        Assert.Equal(0, pulse.ValueAt(11.5e-3));
        Assert.Equal(0, pulse.ValueAt(13e-3));
    }

    [Fact]
    public void PulseWithZeroRiseJumpsStraightToV2()
    {
        var pulse = new PulseWaveform(0, 5, 0, 0, 0, 1e-3, 2e-3);

        Assert.Equal(5, pulse.ValueAt(0));
        Assert.Equal(5, pulse.ValueAt(0.5e-3));
        Assert.Equal(0, pulse.ValueAt(1e-3));
    }

    [Fact]
    public void SineIsOffsetPlusAmplitudeTimesSineOfTwoPiFrequencyTime()
    {
        var sine = new SineWaveform(1, 2, 1000);

        Approximately.Equal(1, sine.ValueAt(0), NgspiceTolerance);
        Approximately.Equal(3, sine.ValueAt(0.25e-3), NgspiceTolerance);
        Approximately.Equal(1, sine.ValueAt(0.5e-3), NgspiceTolerance);
        Approximately.Equal(-1, sine.ValueAt(0.75e-3), NgspiceTolerance);
    }

    [Fact]
    public void SineHoldsOffsetPlusAmplitudeTimesSineOfThePhaseBeforeItsDelay()
    {
        var sine = new SineWaveform(0, 10, 50, Delay: 1e-3, Theta: 0, PhaseDegrees: 90);

        Approximately.Equal(10, sine.ValueAt(0), NgspiceTolerance);
        Approximately.Equal(10, sine.ValueAt(0.5e-3), NgspiceTolerance);
        Approximately.Equal(10, sine.ValueAt(1e-3), NgspiceTolerance);
    }

    [Fact]
    public void SineDampsByExpOfMinusThetaTimesTheTimeSinceItsDelay()
    {
        var sine = new SineWaveform(0, 1, 1000, Delay: 1e-3, Theta: 500);
        var expected = Math.Exp(-500 * 0.25e-3) * Math.Sin(Math.PI / 2);

        Approximately.Equal(expected, sine.ValueAt(1.25e-3), NgspiceTolerance);
    }

    [Fact]
    public void SinePhaseIsMeasuredInDegrees()
    {
        var sine = new SineWaveform(0, 1, 1000, Delay: 0, Theta: 0, PhaseDegrees: 90);

        Approximately.Equal(1, sine.ValueAt(0), NgspiceTolerance);
        Assert.True(Math.Abs(sine.ValueAt(0.25e-3)) < 1e-12);
    }

    [Fact]
    public void SineStopsAfterItsCycleCount()
    {
        var sine = new SineWaveform(0, 1, 1000, Cycles: 2);

        Approximately.Equal(1, sine.ValueAt(1.25e-3), NgspiceTolerance);
        Assert.Equal(0, sine.ValueAt(2e-3));
        Assert.Equal(0, sine.ValueAt(5e-3));
    }

    [Theory]
    [InlineData(0.0005, 0)]
    [InlineData(0.000991, 0)]
    [InlineData(0.001485, 0.62091696)]
    [InlineData(0.001985, 0.86054314)]
    [InlineData(0.002485, 0.94869669)]
    [InlineData(0.003475, 0.37965761)]
    [InlineData(0.004475, 0.05138107)]
    public void ExpFollowsTheNgspiceRisingAndFallingExponentials(double time, double expected)
    {
        var exponential = new ExpWaveform(0, 1, 1e-3, 0.5e-3, 3e-3, 0.5e-3);

        Approximately.Equal(expected, exponential.ValueAt(time), NgspiceTolerance);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0.0005, 1)]
    [InlineData(0.001, 1)]
    [InlineData(0.001485, 1.97)]
    [InlineData(0.0015, 2)]
    [InlineData(0.002, 3)]
    [InlineData(0.004, 3)]
    public void PwlInterpolatesAndHoldsTheFirstAndLastValues(double time, double expected)
    {
        var piecewise = new PwlWaveform(
            [new WaveformPoint(1e-3, 1), new WaveformPoint(2e-3, 3)]);

        Approximately.Equal(expected, piecewise.ValueAt(time), NgspiceTolerance);
    }

    [Fact]
    public void PwlWithASinglePointIsConstant()
    {
        var piecewise = new PwlWaveform([new WaveformPoint(1e-3, 2.5)]);

        Assert.Equal(2.5, piecewise.ValueAt(0));
        Assert.Equal(2.5, piecewise.ValueAt(1e-3));
        Assert.Equal(2.5, piecewise.ValueAt(1));
    }
}
