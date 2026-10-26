using Nodal.Foundation.Waveforms;
using Xunit;

namespace Nodal.Foundation.Tests.Waveforms;

public class WaveformValidationTests
{
    [Fact]
    public void AValidPulseReportsNoError()
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-6, 1e-6, 1e-3, 2e-3);

        Assert.Equal(WaveformError.None, pulse.Validate());
        Assert.True(pulse.IsValid);
    }

    [Theory]
    [InlineData(-1e-3, 1e-6, 1e-6, 1e-3, 2e-3)]
    [InlineData(0, -1e-6, 1e-6, 1e-3, 2e-3)]
    [InlineData(0, 1e-6, -1e-6, 1e-3, 2e-3)]
    [InlineData(0, 1e-6, 1e-6, -1e-3, 2e-3)]
    [InlineData(0, 1e-6, 1e-6, 1e-3, -2e-3)]
    public void ANegativeTimeOrDurationIsAnError(
        double delay, double rise, double fall, double on, double period)
    {
        var pulse = new PulseWaveform(0, 5, delay, rise, fall, on, period);

        Assert.Equal(WaveformError.NegativeTime, pulse.Validate());
    }

    [Fact]
    public void APeriodShorterThanRisePlusOnPlusFallIsAnError()
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-3, 1e-3, 1e-3, 2.9e-3);

        Assert.Equal(WaveformError.PeriodTooShort, pulse.Validate());
    }

    [Fact]
    public void APeriodExactlyRisePlusOnPlusFallIsAccepted()
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-3, 1e-3, 1e-3, 3e-3);

        Assert.Equal(WaveformError.None, pulse.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ACycleCountThatIsNotPositiveIsAnError(int cycles)
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-6, 1e-6, 1e-3, 2e-3, cycles);

        Assert.Equal(WaveformError.InvalidCycleCount, pulse.Validate());
    }

    [Fact]
    public void ANegativeSineDelayIsAnError()
    {
        var sine = new SineWaveform(0, 1, 1000, Delay: -1e-3);

        Assert.Equal(WaveformError.NegativeTime, sine.Validate());
    }

    [Fact]
    public void ASineCycleCountThatIsNotPositiveIsAnError()
    {
        var sine = new SineWaveform(0, 1, 1000, Cycles: 0);

        Assert.Equal(WaveformError.InvalidCycleCount, sine.Validate());
    }

    [Theory]
    [InlineData(0, 1e-3)]
    [InlineData(1e-3, 0)]
    [InlineData(-1e-3, 1e-3)]
    public void ANonPositiveTimeConstantIsAnError(double riseTau, double fallTau)
    {
        var exponential = new ExpWaveform(0, 1, 1e-3, riseTau, 2e-3, fallTau);

        Assert.Equal(WaveformError.NonPositiveTau, exponential.Validate());
    }

    [Fact]
    public void ANegativeExpDelayIsAnError()
    {
        var exponential = new ExpWaveform(0, 1, -1e-3, 1e-3, 2e-3, 1e-3);

        Assert.Equal(WaveformError.NegativeTime, exponential.Validate());
    }

    [Fact]
    public void APiecewiseFunctionWithoutPointsIsAnError()
    {
        Assert.Equal(WaveformError.NoPoints, new PwlWaveform([]).Validate());
    }

    [Fact]
    public void PiecewiseTimesMustStrictlyIncrease()
    {
        var repeated = new PwlWaveform(
            [new WaveformPoint(1e-3, 0), new WaveformPoint(1e-3, 5)]);

        var decreasing = new PwlWaveform(
            [new WaveformPoint(2e-3, 0), new WaveformPoint(1e-3, 5)]);

        Assert.Equal(WaveformError.UnorderedPoints, repeated.Validate());
        Assert.Equal(WaveformError.UnorderedPoints, decreasing.Validate());
    }

    [Fact]
    public void ANegativePiecewiseTimeIsAnError()
    {
        var piecewise = new PwlWaveform([new WaveformPoint(-1e-3, 0)]);

        Assert.Equal(WaveformError.NegativeTime, piecewise.Validate());
    }

    [Fact]
    public void ANonFiniteNumberIsOutOfRange()
    {
        var pulse = new PulseWaveform(0, double.PositiveInfinity, 0, 0, 0, 1e-3, 2e-3);

        Assert.Equal(WaveformError.NumberOutOfRange, pulse.Validate());
    }

    [Fact]
    public void EveryErrorHasAMessage()
    {
        foreach (var error in Enum.GetValues<WaveformError>())
        {
            if (error == WaveformError.None)
            {
                Assert.Empty(WaveformErrorText.Of(error));

                continue;
            }

            Assert.NotEmpty(WaveformErrorText.Of(error));
        }
    }
}
