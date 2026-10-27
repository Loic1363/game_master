using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransistorTransientTests
{
    private const double Supply = 5;
    private const double Load = 100e-12;
    private const double Period = 2e-6;

    private static PulseWaveform Square() =>
        new(0, Supply, 0, 1e-9, 1e-9, Period / 2, Period);

    [Fact]
    public void ACmosInverterDrivenByAPulseInvertsItsInput()
    {
        var result = Inverter();

        Assert.True(result.IsSolved, Listed(result));

        Approximately.Zero(result.VoltageAt(OutputNet(), 0.4 * Period), 1e-2);
        Approximately.Equal(
            Supply, result.VoltageAt(OutputNet(), 0.9 * Period), 1e-3);
    }

    [Fact]
    public void ACmosInverterChargesItsLoadThroughThePmos()
    {
        var result = Inverter();
        var pmos = result.Of("M1");

        Assert.True(result.IsSolved, Listed(result));
        Assert.True(
            result.CurrentAt(pmos.Id, "drain", 0.51 * Period) < 0,
            "The P-channel drain must source current into the load while the output rises.");
    }

    [Fact]
    public void EveryPinOfATransistorHasItsOwnCurrentSeries()
    {
        var series = Inverter().Of("M2");

        Assert.Equal(
            new[] { "drain", "source", "gate" }, series.Pins.Select(pin => pin.Name));

        foreach (var pin in series.Pins)
        {
            Assert.Equal(series.Currents.Length, pin.Currents.Length);
        }
    }

    [Fact]
    public void ThePinCurrentsOfATransistorSumToZeroAtEveryStep()
    {
        var series = Inverter().Of("M2");

        for (var index = 0; index < series.Currents.Length; index++)
        {
            Approximately.Zero(series.Pins.Sum(pin => pin.Currents[index]), 1e-15);
        }
    }

    [Fact]
    public void TheComponentCurrentIsTheDrainPinCurrent()
    {
        var result = Inverter();
        var series = result.Of("M2");

        Assert.True(series.TryGetPin("drain", out var drain));
        Assert.Equal(series.Currents, drain.Currents);
        Assert.Equal(
            result.CurrentAt(series.Id, 0.9 * Period),
            result.CurrentAt(series.Id, "drain", 0.9 * Period));
    }

    [Fact]
    public void TheGatePinCarriesNoCurrentWithoutCapacitances()
    {
        var series = Inverter().Of("M2");

        Assert.True(series.TryGetPin("gate", out var gate));
        Assert.All(gate.Currents, current => Assert.Equal(0, current));
    }

    [Fact]
    public void ATransistorReportsItsRegionOverTime()
    {
        var result = Inverter();
        var nmos = result.Of("M2");

        Assert.Equal(
            DeviceRegion.Linear, result.DeviceAt(nmos.Id, 0.4 * Period)?.Region);

        Assert.Equal(
            DeviceRegion.Cutoff, result.DeviceAt(nmos.Id, 0.95 * Period)?.Region);
    }

    [Fact]
    public void ATwoTerminalComponentReportsNoRegionOverTime()
    {
        var result = Inverter();

        Assert.Null(result.DeviceAt(result.Of("C1").Id, 0.5 * Period));
    }

    [Fact]
    public void AnNpnSwitchSaturatesWhenItsBaseIsDriven()
    {
        var result = TransistorTransientCircuits
            .NpnSwitch(Supply, 1000, 10000, Square())
            .Transient(new TransientOptions(2 * Period) { MaxStep = Period / 1000 });

        Assert.True(result.IsSolved, Listed(result));

        var transistor = result.Of("Q1");
        var driven = result.DeviceAt(transistor.Id, 0.4 * Period);
        var off = result.DeviceAt(transistor.Id, 0.9 * Period);

        Assert.Equal(DeviceRegion.Saturation, driven?.Region);
        Assert.Equal(DeviceRegion.Cutoff, off?.Region);
        Assert.True(
            result.CurrentAt(transistor.Id, "collector", 0.4 * Period) > 1e-3,
            "A saturated switch must carry the load current.");
    }

    private static TransientResult Inverter() =>
        TransistorTransientCircuits.CmosInverter(Supply, Load, Square())
            .Transient(new TransientOptions(2 * Period) { MaxStep = Period / 1000 });

    private static string OutputNet() =>
        TransistorTransientCircuits.CmosInverter(Supply, Load, Square()).NetAt("M2", "drain");

    private static string Listed(TransientResult result) =>
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message));
}
