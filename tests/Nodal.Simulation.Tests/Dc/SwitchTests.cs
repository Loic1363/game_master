using Nodal.Circuit.Schematics;
using Nodal.Foundation.Schedules;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class SwitchTests
{
    private const double RelativeTolerance = 1e-9;
    private const double CurrentFloor = 1e-15;

    [Fact]
    public void AClosedSwitchCarriesTheSupplyThroughItsOnResistance()
    {
        var solution = ControlCircuits.SwitchLoad(5, 5, 1000).Solve();
        var contacts = solution.Of("S1");

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(5.0 / 1001, contacts.Current.Value, RelativeTolerance);
        Approximately.Equal(5.0 / 1001, contacts.Voltage.Value, RelativeTolerance);
        Assert.Equal(DeviceRegion.On, contacts.Device!.Region);
    }

    [Fact]
    public void AnOpenSwitchLeaksThroughItsOffResistance()
    {
        var solution = ControlCircuits.SwitchLoad(0, 5, 1000).Solve();
        var contacts = solution.Of("S1");

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(5.0 / 1.000001e9, contacts.Current.Value, RelativeTolerance);
        Approximately.Equal(5 * 1e9 / 1.000001e9, contacts.Voltage.Value, RelativeTolerance);
        Assert.Equal(DeviceRegion.Off, contacts.Device!.Region);
    }

    [Theory]
    [InlineData(5.0, DeviceRegion.On)]
    [InlineData(3.6, DeviceRegion.On)]
    [InlineData(3.0, DeviceRegion.Off)]
    [InlineData(2.5, DeviceRegion.Off)]
    [InlineData(1.4, DeviceRegion.Off)]
    public void InsideTheHysteresisBandADcSwitchStartsFromOpen(
        double control, DeviceRegion expected)
    {
        var solution = Hysteresis(control, 1).Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Assert.Equal(expected, solution.Of("S1").Device!.Region);
    }

    [Fact]
    public void TheSwitchOnlyClosesAboveTheThresholdPlusTheHysteresis()
    {
        Assert.Equal(DeviceRegion.Off, Hysteresis(3.5, 1).Solve().Of("S1").Device!.Region);
        Assert.Equal(DeviceRegion.On, Hysteresis(3.51, 1).Solve().Of("S1").Device!.Region);
    }

    [Fact]
    public void EveryPinCurrentOfASwitchSumsToZeroAndItsControlDrawsNothing()
    {
        var contacts = ControlCircuits.SwitchLoad(5, 5, 1000).Solve().Of("S1");

        Assert.Equal(4, contacts.Pins.Length);
        Assert.True(contacts.TryGetPin("control-plus", out var control));
        Approximately.Zero(control.Current.Value, CurrentFloor);
        Approximately.Zero(contacts.Pins.Sum(pin => pin.Current.Value), CurrentFloor);
    }

    [Fact]
    public void ASwitchThatKeepsChangingStateHasNoOperatingPoint()
    {
        var solution = ControlCircuits.SwitchRelaxation(5, 10000)
            .Set("S1", "on-resistance", 100)
            .Set("S1", "threshold", 2.5)
            .Solve();

        Assert.False(solution.IsSolved);

        var diagnostic = Assert.Single(solution.Diagnostics);

        Assert.Equal(SimulationDiagnosticCode.SwitchInstability, diagnostic.Code);
        Assert.Equal(
            "No stable operating point: switch S1 keeps changing state.", diagnostic.Message);
    }

    [Fact]
    public void APressedButtonCarriesTheSupplyThroughItsOnResistance()
    {
        var solution = ControlCircuits.PushButtonLoad(PressSchedule.Pressed, 5, 1000).Solve();
        var button = solution.Of("SW1");

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(5.0 / 1000.01, button.Current.Value, RelativeTolerance);
        Assert.Equal(DeviceRegion.On, button.Device!.Region);
    }

    [Fact]
    public void AReleasedButtonLeaksThroughItsOffResistance()
    {
        var solution = ControlCircuits.PushButtonLoad(PressSchedule.Released, 5, 1000).Solve();
        var button = solution.Of("SW1");

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(5.0 / 1.000001e9, button.Current.Value, RelativeTolerance);
        Assert.Equal(DeviceRegion.Off, button.Device!.Region);
    }

    [Fact]
    public void AButtonPressedFromZeroIsClosedInTheOperatingPoint()
    {
        var solution = ControlCircuits
            .PushButtonLoad(Parsed("0 to 1m"), 5, 1000)
            .Solve();

        Assert.Equal(DeviceRegion.On, solution.Of("SW1").Device!.Region);
    }

    [Fact]
    public void AButtonPressedLaterIsOpenInTheOperatingPoint()
    {
        var solution = ControlCircuits
            .PushButtonLoad(Parsed("1m to 2m"), 5, 1000)
            .Solve();

        Assert.Equal(DeviceRegion.Off, solution.Of("SW1").Device!.Region);
    }

    [Fact]
    public void AButtonHasTwoPinsWhoseCurrentsCancel()
    {
        var button = ControlCircuits.PushButtonLoad(PressSchedule.Pressed, 5, 1000)
            .Solve()
            .Of("SW1");

        Assert.Equal(2, button.Pins.Length);
        Approximately.Zero(button.Pins.Sum(pin => pin.Current.Value), CurrentFloor);
    }

    private static Schematic Hysteresis(double control, double hysteresis) =>
        ControlCircuits.SwitchLoad(control, 5, 1000)
            .Set("S1", "threshold", 2.5)
            .Set("S1", "hysteresis", hysteresis);

    private static PressSchedule Parsed(string text)
    {
        Assert.True(PressScheduleText.TryParse(text, out var schedule, out _), text);

        return schedule!;
    }

    private static string Listed(OperatingPoint solution) =>
        string.Join("; ", solution.Diagnostics.Select(diagnostic => diagnostic.Message));
}
