using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class BreakpointTests
{
    private const double Load = 1000;

    private static TransientResult Driven(Waveform waveform, double stopTime) =>
        TransientCircuits.SourceAndResistor(Load)
            .Function("V1", waveform)
            .Transient(new TransientOptions(stopTime));

    private static void AssertLandsOnEveryCorner(Waveform waveform, double stopTime)
    {
        var result = Driven(waveform, stopTime);
        var corners = waveform.BreakpointsUntil(stopTime).ToList();

        Assert.True(result.IsSolved);
        Assert.NotEmpty(corners);

        foreach (var corner in corners)
        {
            Assert.Contains(corner, result.Times);
        }
    }

    [Fact]
    public void EveryPulseCornerIsATimePoint() => AssertLandsOnEveryCorner(
        new PulseWaveform(0, 5, 1e-6, 1e-6, 1e-6, 2e-6, 10e-6), 25e-6);

    [Fact]
    public void EveryCornerOfASharpPulseIsATimePoint() => AssertLandsOnEveryCorner(
        new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 2e-6, 4e-6), 10e-6);

    [Fact]
    public void EveryPwlPointIsATimePoint() => AssertLandsOnEveryCorner(
        new PwlWaveform(
        [
            new WaveformPoint(1e-6, 0),
            new WaveformPoint(3e-6, 5),
            new WaveformPoint(7e-6, 2),
        ]),
        10e-6);

    [Fact]
    public void BothExponentialDelaysAreTimePoints() => AssertLandsOnEveryCorner(
        new ExpWaveform(0, 5, 1e-6, 2e-7, 4e-6, 3e-7), 10e-6);

    [Fact]
    public void TheSineDelayIsATimePoint() => AssertLandsOnEveryCorner(
        new SineWaveform(0, 5, 1e6, 2e-6), 10e-6);

    [Fact]
    public void TheEndOfASineBurstIsATimePoint() => AssertLandsOnEveryCorner(
        new SineWaveform(0, 5, 1e6, 1e-6, 0, 0, 3), 10e-6);

    [Fact]
    public void ThePulseCornersAreAlsoHitWhenACapacitorSmoothsTheEdges()
    {
        var pulse = new PulseWaveform(0, 5, 1e-6, 1e-7, 1e-7, 2e-6, 5e-6);
        var stopTime = 20e-6;

        var result = TransientCircuits.Rc(Load, 100e-9)
            .Function("V1", pulse)
            .Transient(new TransientOptions(stopTime));

        Assert.True(result.IsSolved);

        foreach (var corner in pulse.BreakpointsUntil(stopTime))
        {
            Assert.Contains(corner, result.Times);
        }
    }

    [Fact]
    public void TheRunStartsAtZeroAndEndsExactlyAtTheStopTime()
    {
        var stopTime = 20e-6;
        var result = Driven(new PulseWaveform(0, 5, 0, 1e-9, 1e-9, 3e-6, 7e-6), stopTime);

        Assert.True(result.IsSolved);
        Assert.Equal(0, result.Times[0]);
        Assert.Equal(stopTime, result.Times[^1]);
        Assert.Equal(stopTime, result.FinalTime);
    }

    [Fact]
    public void TheTimePointsIncreaseStrictly()
    {
        var pulse = new PulseWaveform(0, 5, 1e-6, 1e-7, 1e-7, 2e-6, 5e-6);

        var result = TransientCircuits.Rc(Load, 100e-9)
            .Function("V1", pulse)
            .Transient(new TransientOptions(20e-6));

        Assert.True(result.IsSolved);

        for (var index = 1; index < result.Times.Length; index++)
        {
            Assert.True(
                result.Times[index] > result.Times[index - 1],
                $"Point {index} at {result.Times[index]:R} does not advance.");
        }
    }

    [Fact]
    public void APulseEdgeIsFollowedImmediatelyByItsNewLevel()
    {
        var result = Driven(new PulseWaveform(0, 5, 2e-6, 1e-9, 1e-9, 5e-6, 20e-6), 20e-6);

        Assert.True(result.IsSolved);
        Approximately.Zero(result.VoltsAt("R1", 2e-6), 1e-9);
        Approximately.Equal(5, result.VoltsAt("R1", 2e-6 + 1e-9), 1e-9);
        Approximately.Equal(5, result.VoltsAt("R1", 4e-6), 1e-9);
        Approximately.Zero(result.VoltsAt("R1", 7.1e-6), 1e-9);
    }
}
