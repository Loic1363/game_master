using Nodal.Circuit.Schematics;
using Nodal.Foundation.Schedules;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Tests.Dc;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class ControlTransientTests
{
    private const double RelativeTolerance = 1e-6;
    private const double CurrentFloor = 1e-12;
    private const double Load = 1000;

    [Fact]
    public void AVoltageControlledVoltageSourceFollowsItsControlOverTime()
    {
        var result = Driven(ControlCircuits.VcvsAmplifier(0, 5, Load), Sine(), 2e-3);
        var source = result.Of("E1");

        Assert.True(result.IsSolved);
        Approximately.Equal(5, result.VoltsAt("E1", 0.25e-3), 1e-3);
        Approximately.Equal(-5, result.VoltsAt("E1", 0.75e-3), 1e-3);
        Approximately.Zero(result.CurrentAt(source.Id, "control-plus", 0.25e-3), CurrentFloor);
    }

    [Fact]
    public void TheOutputPinCurrentOfAControlledSourceIsTheDeliveredCurrentReversed()
    {
        var result = Driven(ControlCircuits.VcvsAmplifier(0, 5, Load), Sine(), 2e-3);
        var source = result.Of("E1");

        Approximately.Equal(
            -5.0 / Load, result.CurrentAt(source.Id, "plus", 0.25e-3), 1e-3);

        Approximately.Equal(
            5.0 / Load, result.CurrentAt(source.Id, "minus", 0.25e-3), 1e-3);
    }

    [Fact]
    public void AControlledSourceDescribesItselfAtATime()
    {
        var result = Driven(ControlCircuits.VcvsAmplifier(0, 5, Load), Sine(), 2e-3);
        var device = result.DeviceAt(result.Of("E1").Id, 0.25e-3);

        Assert.NotNull(device);
        Assert.Null(device.Region);
        Assert.Equal(
            [
                DeviceQuantityNames.ControlVoltage,
                DeviceQuantityNames.OutputVoltage,
                DeviceQuantityNames.OutputCurrent,
            ],
            device.Quantities.Select(quantity => quantity.Name));

        Approximately.Equal(5, device.Quantities[1].Value.Value, 1e-3);
        Approximately.Equal(5.0 / Load, device.Quantities[2].Value.Value, 1e-3);
    }

    [Fact]
    public void ACurrentControlledSourceSensesItsControlPinsOverTime()
    {
        var result = Driven(
            ControlCircuits.CccsLoad(0, 1000, 10, 100), Sine(), 2e-3);

        var source = result.Of("F1");

        Approximately.Equal(
            1e-3, result.CurrentAt(source.Id, "control-plus", 0.25e-3), 1e-3);

        Approximately.Equal(
            -10e-3, result.CurrentAt(source.Id, "plus", 0.25e-3), 1e-3);
    }

    [Fact]
    public void APushButtonSwitchesExactlyAtItsScheduledInstants()
    {
        var result = PushButton("1m to 3m", 5e-3);
        var button = result.Of("SW1");

        Assert.True(result.IsSolved);
        Assert.False(result.IsClosedAt(button.Id, 0.5e-3));
        Assert.True(result.IsClosedAt(button.Id, 1e-3));
        Assert.True(result.IsClosedAt(button.Id, 2e-3));
        Assert.False(result.IsClosedAt(button.Id, 3e-3));
        Assert.False(result.IsClosedAt(button.Id, 4e-3));
    }

    [Fact]
    public void TheScheduledInstantsAreBreakpointsOfTheRun()
    {
        var result = PushButton("1m to 3m", 5e-3);

        Assert.Contains(1e-3, result.Times);
        Assert.Contains(3e-3, result.Times);
    }

    [Fact]
    public void APressedButtonCarriesItsCurrentAndAReleasedOneLeaks()
    {
        var result = PushButton("1m to 3m", 5e-3);

        Approximately.Equal(5.0 / 1000.01, result.AmperesAt("SW1", 2e-3), RelativeTolerance);
        Approximately.Equal(5.0 / 1.000001e9, result.AmperesAt("SW1", 4e-3), RelativeTolerance);
    }

    [Fact]
    public void APushButtonDescribesItselfAsPressedOrReleased()
    {
        var result = PushButton("1m to 3m", 5e-3);
        var id = result.Of("SW1").Id;

        Assert.Equal(DeviceRegion.On, result.DeviceAt(id, 2e-3)!.Region);
        Assert.Equal(DeviceRegion.Off, result.DeviceAt(id, 4e-3)!.Region);

        Assert.Equal(
            [DeviceQuantityNames.Voltage, DeviceQuantityNames.Current],
            result.DeviceAt(id, 2e-3)!.Quantities.Select(quantity => quantity.Name));
    }

    [Fact]
    public void AClosedSwitchStaysClosedInsideTheHysteresisBand()
    {
        var result = Driven(
            Hysteresis(), new PwlWaveform([new WaveformPoint(0, 5), new WaveformPoint(1e-3, 3)]),
            2e-3);

        var id = result.Of("S1").Id;

        Assert.True(result.IsSolved);
        Assert.True(result.IsClosedAt(id, 0));
        Assert.True(result.IsClosedAt(id, 1.5e-3));
        Assert.Equal(DeviceRegion.On, result.DeviceAt(id, 1.5e-3)!.Region);
    }

    [Fact]
    public void AnOpenSwitchStaysOpenInsideTheHysteresisBand()
    {
        var result = Driven(
            Hysteresis(), new PwlWaveform([new WaveformPoint(0, 0), new WaveformPoint(1e-3, 3)]),
            2e-3);

        var id = result.Of("S1").Id;

        Assert.True(result.IsSolved);
        Assert.False(result.IsClosedAt(id, 0));
        Assert.False(result.IsClosedAt(id, 1.5e-3));
        Assert.Equal(DeviceRegion.Off, result.DeviceAt(id, 1.5e-3)!.Region);
    }

    [Fact]
    public void ASwitchFollowsItsControlPulse()
    {
        var result = Driven(
            ControlCircuits.SwitchLoad(0, 5, Load),
            new PulseWaveform(0, 5, 1e-3, 1e-6, 1e-6, 1e-3, 4e-3),
            4e-3);

        var id = result.Of("S1").Id;

        Assert.False(result.IsClosedAt(id, 0.5e-3));
        Assert.True(result.IsClosedAt(id, 1.5e-3));
        Assert.False(result.IsClosedAt(id, 3e-3));

        Approximately.Equal(5.0 / 1001, result.AmperesAt("S1", 1.5e-3), RelativeTolerance);
    }

    [Fact]
    public void TheControlPinsOfASwitchDrawNoCurrentOverTime()
    {
        var result = Driven(
            ControlCircuits.SwitchLoad(5, 5, Load),
            new PulseWaveform(0, 5, 0, 1e-6, 1e-6, 1e-3, 4e-3),
            2e-3);

        var id = result.Of("S1").Id;

        Assert.Equal(4, result.Of("S1").Pins.Length);
        Approximately.Zero(result.CurrentAt(id, "control-plus", 1e-3), CurrentFloor);
        Approximately.Zero(result.CurrentAt(id, "control-minus", 1e-3), CurrentFloor);
    }

    private static Schematic Hysteresis() =>
        ControlCircuits.SwitchLoad(0, 5, Load)
            .Set("S1", "threshold", 2.5)
            .Set("S1", "hysteresis", 1);

    private static Waveform Sine() => new SineWaveform(0, 1, 1000);

    private static TransientResult Driven(Schematic schematic, Waveform control, double stopTime) =>
        schematic.Function("V1", control).Transient(new TransientOptions(stopTime));

    private static TransientResult PushButton(string presses, double stopTime)
    {
        Assert.True(PressScheduleText.TryParse(presses, out var schedule, out _), presses);

        return ControlCircuits.PushButtonLoad(schedule!, 5, Load)
            .Transient(new TransientOptions(stopTime));
    }
}
