using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientDeviceTests
{
    private const double Load = 1000;
    private const double MainsFrequency = 50;
    private const double MainsAmplitude = 10;

    private static SineWaveform Mains() => new(0, MainsAmplitude, MainsFrequency);

    [Fact]
    public void AnUnderdampedRlcOvershootsByTheAnalyticalAmount()
    {
        var resistance = 10.0;
        var inductance = 1e-3;
        var capacitance = 1e-6;
        var damping = resistance / 2 * Math.Sqrt(capacitance / inductance);
        var damped = Math.Sqrt(1 - (damping * damping));
        var expectedPeak = 1 + Math.Exp(-Math.PI * damping / damped);
        var expectedTime = Math.PI * Math.Sqrt(inductance * capacitance) / damped;

        var result = TransientCircuits.Rlc(resistance, inductance, capacitance)
            .Set("V1", "voltage", 1)
            .Transient(new TransientOptions(1e-3)
            {
                StartFromZero = true,
                MaxStep = 100e-9,
            });

        Assert.True(result.IsSolved);

        var voltages = result.Of("C1").Voltages;
        var peak = voltages.Max();
        var peakTime = result.Times[voltages.IndexOf(peak)];

        Approximately.Equal(expectedPeak, peak, 1e-4);
        Approximately.Equal(expectedTime, peakTime, 1e-3);
    }

    [Fact]
    public void AnUnderdampedRlcRingsAroundTheFinalValue()
    {
        var result = TransientCircuits.Rlc(10, 1e-3, 1e-6)
            .Set("V1", "voltage", 1)
            .Transient(new TransientOptions(5e-3)
            {
                StartFromZero = true,
                MaxStep = 100e-9,
            });

        Assert.True(result.IsSolved);
        Approximately.Equal(1, result.VoltsAt("C1", 5e-3), 1e-3);
        Approximately.Zero(result.AmperesAt("L1", 5e-3), 1e-6);
    }

    [Fact]
    public void AHalfWaveRectifierBlocksTheNegativeHalfCycle()
    {
        var result = TransientCircuits.HalfWaveRectifier(Load)
            .Function("V1", Mains())
            .Transient(new TransientOptions(40e-3) { MaxStep = 10e-6 });

        Assert.True(result.IsSolved);

        var voltages = result.Of("R1").Voltages;

        Assert.True(voltages.Min() > -1e-3);
        Approximately.Equal(MainsAmplitude, voltages.Max(), 0.1);
    }

    [Fact]
    public void AHalfWaveRectifierLosesAboutADiodeDropAtThePeak()
    {
        var result = TransientCircuits.HalfWaveRectifier(Load)
            .Function("V1", Mains())
            .Transient(new TransientOptions(40e-3) { MaxStep = 10e-6 });

        var peak = result.Of("R1").Voltages.Max();
        var drop = MainsAmplitude - peak;

        Assert.True(drop > 0.3, $"The diode dropped only {drop:R} V.");
        Assert.True(drop < 0.8, $"The diode dropped {drop:R} V.");
        Approximately.Equal(peak / Load, result.Of("R1").Currents.Max(), 1e-9);
    }

    [Fact]
    public void ASmoothingCapacitorHoldsTheRectifiedOutputUp()
    {
        var result = TransientCircuits.SmoothedRectifier(Load, 100e-6)
            .Function("V1", Mains())
            .Transient(new TransientOptions(100e-3) { MaxStep = 20e-6 });

        Assert.True(result.IsSolved);

        var settled = result.Times
            .Select((time, index) => (time, index))
            .Where(point => point.time > 60e-3)
            .Select(point => result.Of("R1").Voltages[point.index])
            .ToList();

        Assert.True(settled.Min() > 7, $"The output sagged to {settled.Min():R} V.");
        Assert.True(settled.Max() < MainsAmplitude);
    }

    [Fact]
    public void ADiodeDoesNotConductBeforeTheSourceRises()
    {
        var result = TransientCircuits.HalfWaveRectifier(Load)
            .Function("V1", new SineWaveform(0, MainsAmplitude, MainsFrequency, 5e-3))
            .Transient(new TransientOptions(20e-3) { MaxStep = 10e-6 });

        Assert.True(result.IsSolved);
        Approximately.Zero(result.VoltsAt("R1", 1e-3), 1e-6);
        Assert.True(result.VoltsAt("R1", 10e-3) > 1);
    }

    [Fact]
    public void AnEquivalentSeriesResistanceLimitsTheInrushCurrent()
    {
        var resistance = 1.0;
        var esr = 9.0;
        var supply = 5.0;

        var result = TransientCircuits.Rc(resistance, 100e-9)
            .Set("V1", "voltage", supply)
            .Set("C1", "esr", esr)
            .Transient(new TransientOptions(1e-4) { StartFromZero = true });

        Assert.True(result.IsSolved);
        Approximately.Equal(
            supply / (resistance + esr), result.AmperesAt("C1", 0), 1e-9);

        Approximately.Equal(
            supply * esr / (resistance + esr), result.VoltsAt("C1", 0), 1e-9);

        Approximately.Equal(supply, result.VoltsAt("C1", 1e-4), 1e-6);
        Approximately.Zero(result.AmperesAt("C1", 1e-4), 1e-9);
    }

    [Fact]
    public void AnEquivalentSeriesInductanceDelaysTheInrushCurrent()
    {
        var result = TransientCircuits.Rc(1, 100e-9)
            .Set("V1", "voltage", 5)
            .Set("C1", "esr", 9)
            .Set("C1", "esl", 1e-6)
            .Transient(new TransientOptions(1e-4) { StartFromZero = true });

        Assert.True(result.IsSolved);
        Approximately.Zero(result.AmperesAt("C1", 0), 1e-12);
        Assert.True(result.AmperesAt("C1", 1e-7) > 0);
        Approximately.Equal(5, result.VoltsAt("C1", 1e-4), 1e-6);
    }

    [Fact]
    public void ALeakyCapacitorSettlesOnTheResistiveDivider()
    {
        var leakage = 1e6;
        var series = 1000.0;
        var supply = 5.0;

        var result = TransientCircuits.Rc(series, 100e-9)
            .Set("V1", "voltage", supply)
            .Set("C1", "parallel-resistance", leakage)
            .Transient(new TransientOptions(2e-3) { StartFromZero = true });

        Assert.True(result.IsSolved);
        Approximately.Equal(
            supply * leakage / (series + leakage), result.VoltsAt("C1", 2e-3), 1e-5);
    }

    [Fact]
    public void TheTransientEndsOnTheOperatingPointOfAStableCircuit()
    {
        var schematic = TransientCircuits.Rc(1000, 100e-9).Set("V1", "voltage", 5);
        var operatingPoint = schematic.Solve();

        var result = schematic.Transient(
            new TransientOptions(2e-3) { StartFromZero = true });

        Assert.True(result.IsSolved);
        Assert.True(operatingPoint.IsSolved);

        foreach (var net in operatingPoint.NetVoltages)
        {
            Approximately.Zero(
                net.Value.Value - result.VoltageAt(net.Key, result.FinalTime), 1e-6);
        }
    }

    [Fact]
    public void ADiodeCarriesTheShockleyCurrentWhileConducting()
    {
        var result = TransientCircuits.HalfWaveRectifier(Load)
            .Function("V1", Mains())
            .Transient(new TransientOptions(40e-3) { MaxStep = 10e-6 });

        var series = result.Of("D1");
        var peak = series.Currents.Max();
        var index = series.Currents.IndexOf(peak);

        Approximately.Equal(
            DiodeModel.Current(series.Voltages[index]), peak, 1e-6);
    }
}
