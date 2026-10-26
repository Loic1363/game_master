using Nodal.Foundation.Tests.Quantities;
using Nodal.Foundation.Waveforms;
using Xunit;

namespace Nodal.Foundation.Tests.Waveforms;

public class WaveformBreakpointTests
{
    [Fact]
    public void PulseBreaksAtEveryRampCornerOfEveryCycle()
    {
        var pulse = new PulseWaveform(0, 5, 1e-3, 1e-4, 1e-4, 1e-3, 3e-3);

        Approximately.EqualSequence(
            [1e-3, 1.1e-3, 2.1e-3, 2.2e-3, 4e-3, 4.1e-3, 5.1e-3, 5.2e-3],
            [.. pulse.BreakpointsUntil(6e-3)]);
    }

    [Fact]
    public void PulseBreaksWhereItStopsPulsing()
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-4, 1e-4, 1e-3, 3e-3, Cycles: 1);

        Approximately.EqualSequence(
            [1e-4, 1.1e-3, 1.2e-3, 3e-3], [.. pulse.BreakpointsUntil(10e-3)]);
    }

    [Fact]
    public void PulseBreakpointsStopAtTheStopTime()
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-4, 1e-4, 1e-3, 3e-3);

        Assert.All(pulse.BreakpointsUntil(4e-3), time => Assert.True(time <= 4e-3));
    }

    [Fact]
    public void SineBreaksAtItsDelay()
    {
        var sine = new SineWaveform(0, 1, 1000, Delay: 2e-3);

        Approximately.EqualSequence([2e-3], [.. sine.BreakpointsUntil(10e-3)]);
    }

    [Fact]
    public void SineBreaksWhereItStopsOscillating()
    {
        var sine = new SineWaveform(0, 1, 1000, Delay: 1e-3, Cycles: 2);

        Approximately.EqualSequence([1e-3, 3e-3], [.. sine.BreakpointsUntil(10e-3)]);
    }

    [Fact]
    public void ExpBreaksAtBothDelays()
    {
        var exponential = new ExpWaveform(0, 1, 1e-3, 0.5e-3, 3e-3, 0.5e-3);

        Approximately.EqualSequence([1e-3, 3e-3], [.. exponential.BreakpointsUntil(10e-3)]);
    }

    [Fact]
    public void PwlBreaksAtEveryPoint()
    {
        var piecewise = new PwlWaveform(
        [
            new WaveformPoint(0, 0),
            new WaveformPoint(1e-3, 5),
            new WaveformPoint(2e-3, 0),
        ]);

        Approximately.EqualSequence([1e-3, 2e-3], [.. piecewise.BreakpointsUntil(10e-3)]);
    }

    [Fact]
    public void NoBreakpointSitsAtZeroOrBeyondTheStopTime()
    {
        var pulse = new PulseWaveform(0, 5, 0, 0, 0, 1e-3, 2e-3);

        Assert.All(
            pulse.BreakpointsUntil(5e-3),
            time => Assert.True(time > 0 && time <= 5e-3));
    }
}
