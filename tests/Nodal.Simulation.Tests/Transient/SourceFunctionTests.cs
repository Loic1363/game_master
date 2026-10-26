using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class SourceFunctionTests
{
    private const double Load = 1000;

    private static void AssertResistorFollows(Waveform waveform, double stopTime)
    {
        var result = TransientCircuits.SourceAndResistor(Load)
            .Function("V1", waveform)
            .Transient(new TransientOptions(stopTime));

        var series = result.Of("R1");

        Assert.True(result.IsSolved);

        for (var index = 0; index < result.PointCount; index++)
        {
            var time = result.Times[index];
            var expected = waveform.ValueAt(time);

            Approximately.Zero(series.Voltages[index] - expected, 1e-9);
            Approximately.Zero(series.Currents[index] - (expected / Load), 1e-12);
        }
    }

    [Fact]
    public void AResistorFollowsAPulseExactly() => AssertResistorFollows(
        new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 20e-6, 40e-6), 100e-6);

    [Fact]
    public void AResistorFollowsASineExactly() => AssertResistorFollows(
        new SineWaveform(0, 10, 50e3), 100e-6);

    [Fact]
    public void AResistorFollowsADampedSineExactly() => AssertResistorFollows(
        new SineWaveform(1, 10, 50e3, 10e-6, 20e3, 45), 100e-6);

    [Fact]
    public void AResistorFollowsAnExponentialExactly() => AssertResistorFollows(
        new ExpWaveform(0, 5, 1e-6, 2e-6, 20e-6, 5e-6), 50e-6);

    [Fact]
    public void AResistorFollowsAPiecewiseLinearSourceExactly() => AssertResistorFollows(
        new PwlWaveform(
        [
            new WaveformPoint(0, 0),
            new WaveformPoint(10e-6, 5),
            new WaveformPoint(20e-6, -5),
            new WaveformPoint(30e-6, 0),
        ]),
        50e-6);

    [Fact]
    public void ACurrentSourceFunctionDrivesTheLoad()
    {
        var pulse = new PulseWaveform(0, 1e-3, 0, 1e-9, 1e-9, 20e-6, 40e-6);

        var schematic = Nodal.Circuit.Schematics.Schematic.Empty
            .Add(Circuits.CurrentSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("R1", "resistance", Load)
            .Function("I1", pulse);

        var result = schematic.Transient(new TransientOptions(100e-6));

        Assert.True(result.IsSolved);
        Approximately.Equal(1, result.VoltsAt("R1", 10e-6), 1e-9);
        Approximately.Zero(result.VoltsAt("R1", 30e-6), 1e-9);
    }

    [Fact]
    public void TheDcOperatingPointUsesTheFunctionValueAtTimeZero()
    {
        var pulse = new PulseWaveform(3, 12, 10e-6, 1e-9, 1e-9, 20e-6, 40e-6);

        var result = TransientCircuits.Rc(Load, 100e-9)
            .Function("V1", pulse)
            .Transient(new TransientOptions(100e-6));

        Assert.True(result.IsSolved);
        Approximately.Equal(3, result.VoltsAt("C1", 0), 1e-9);
    }

    [Fact]
    public void APulseFallingEdgeDischargesTheCapacitorFromTheOperatingPoint()
    {
        var timeConstant = Load * 100e-9;
        var edge = 5 * timeConstant;
        var pulse = new PulseWaveform(0, 5, 0, 1e-9, 1e-9, edge, 1);

        var result = TransientCircuits.Rc(Load, 100e-9)
            .Function("V1", pulse)
            .Transient(new TransientOptions(7 * timeConstant)
            {
                MaxStep = timeConstant / 1000,
            });

        Assert.True(result.IsSolved);

        var charged = result.VoltsAt("C1", edge);

        Approximately.Equal(5 * (1 - Math.Exp(-5)), charged, 1e-4);
        Approximately.Equal(
            charged * Math.Exp(-1), result.VoltsAt("C1", edge + timeConstant), 1e-4);
    }

    [Fact]
    public void APulseWidthModulatedSourceAveragesThroughALowPassFilter()
    {
        var duty = 0.25;
        var period = 1e-6;
        var amplitude = 5.0;
        var timeConstant = Load * 100e-9;

        var pulse = new PulseWaveform(
            0, amplitude, 0, 1e-9, 1e-9, duty * period, period);

        var result = TransientCircuits.Rc(Load, 100e-9)
            .Function("V1", pulse)
            .Transient(new TransientOptions(10 * timeConstant) { StartFromZero = true });

        Assert.True(result.IsSolved);
        Approximately.Equal(
            duty * amplitude, result.VoltsAt("C1", result.FinalTime), 0.01);
    }
}
