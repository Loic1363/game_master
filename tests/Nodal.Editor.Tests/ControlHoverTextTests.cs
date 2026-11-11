using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Foundation.Schedules;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Editor.Tests;

public class ControlHoverTextTests
{
    [Fact]
    public void AVoltageControlledVoltageSourceReadsItsControlAndItsOutput() =>
        Assert.Equal(
            "E1 (Voltage-controlled voltage source): Vctrl 500 mV, Vout 5 V, Iout 5 mA",
            Describe(VoltageControlled("vcvs", 0.5).Set("E1", "gain", 10), "E1"));

    [Fact]
    public void AVoltageControlledCurrentSourceReadsItsCurrentBeforeItsVoltage() =>
        Assert.Equal(
            "G1 (Voltage-controlled current source): Vctrl 1 V, Iout 1 mA, Vout 2 V",
            Describe(
                VoltageControlled("vccs", 1)
                    .Set("G1", "transconductance", 1e-3)
                    .Set("R1", "resistance", 2000),
                "G1"));

    [Fact]
    public void ACurrentControlledCurrentSourceReadsItsControlCurrent() =>
        Assert.Equal(
            "F1 (Current-controlled current source): Ictrl 1 mA, Iout 10 mA, Vout 1 V",
            Describe(
                CurrentControlled("cccs", 1).Set("F1", "gain", 10).Set("R2", "resistance", 100),
                "F1"));

    [Fact]
    public void ACurrentControlledVoltageSourceReadsItsControlCurrent() =>
        Assert.Equal(
            "H1 (Current-controlled voltage source): Ictrl 1 mA, Vout 2 V, Iout 2 mA",
            Describe(
                CurrentControlled("ccvs", 1)
                    .Set("H1", "transresistance", 2000)
                    .Set("R2", "resistance", 1000),
                "H1"));

    [Fact]
    public void AClosedSwitchReadsOn() =>
        Assert.Equal(
            "S1 (Voltage-controlled switch): on, Vctrl 5 V, V 5 mV, I 5 mA",
            Describe(SwitchBox(5), "S1"));

    [Fact]
    public void AnOpenSwitchReadsOff() =>
        Assert.Equal(
            "S1 (Voltage-controlled switch): off, Vctrl 0 V, V 5 V, I 5 nA",
            Describe(SwitchBox(0), "S1"));

    [Fact]
    public void APressedButtonReadsPressed() =>
        Assert.Equal(
            "SW1 (Push button): pressed, V 50 µV, I 5 mA",
            Describe(ButtonBox(PressSchedule.Pressed), "SW1"));

    [Fact]
    public void AReleasedButtonReadsReleased() =>
        Assert.Equal(
            "SW1 (Push button): released, V 5 V, I 5 nA",
            Describe(ButtonBox(PressSchedule.Released), "SW1"));

    private static Schematic VoltageControlled(string definitionId, double control) =>
        Schematic.Empty
            .Add("voltage-source", -8, 0)
            .Add("ground", -8, 2)
            .Add(definitionId, 0, 0)
            .Add("ground", 0, 2)
            .Add("resistor", 6, 0, Sessions.FirstPinUp)
            .Add("ground", 6, 2)
            .Wire(-8, -2, -2, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, -8, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", control)
            .Set("R1", "resistance", 1000);

    private static Schematic CurrentControlled(string definitionId, double source) =>
        Schematic.Empty
            .Add("voltage-source", -14, 0)
            .Add("ground", -14, 2)
            .Add("resistor", -8, -2)
            .Add(definitionId, 0, 0)
            .Add("ground", 0, 2)
            .Add("resistor", 6, 0, Sessions.FirstPinUp)
            .Add("ground", 6, 2)
            .Wire(-14, -2, -10, -2)
            .Wire(-6, -2, -2, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, -14, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", source)
            .Set("R1", "resistance", 1000);

    private static Schematic SwitchBox(double control) =>
        Schematic.Empty
            .Add("voltage-source", -8, 0)
            .Add("ground", -8, 2)
            .Add("switch", 0, 0)
            .Add("voltage-source", 6, 0)
            .Add("ground", 6, 2)
            .Add("resistor", 0, 6, Sessions.FirstPinUp)
            .Add("ground", 0, 8)
            .Wire(-8, -2, -2, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, -8, 2)
            .Wire(6, -2, 0, -2)
            .Wire(0, 2, 0, 4)
            .Set("V1", "voltage", control)
            .Set("V2", "voltage", 5)
            .Set("R1", "resistance", 1000);

    private static Schematic ButtonBox(PressSchedule presses) =>
        Schematic.Empty
            .Add("voltage-source", -8, 0)
            .Add("ground", -8, 2)
            .Add("push-button", 0, 0)
            .Add("resistor", 6, 0)
            .Add("ground", 8, 0)
            .Wire(-8, -2, -2, -2)
            .Wire(-2, -2, -2, 0)
            .Wire(2, 0, 4, 0)
            .Set("V1", "voltage", 5)
            .Set("R1", "resistance", 1000)
            .Presses("SW1", presses);

    private static string? Describe(Schematic schematic, string designator)
    {
        var solution = DcAnalysis.Solve(
            NetlistCompiler.Compile(schematic, BaseContent.Catalog), BaseContent.Catalog);

        Assert.True(
            solution.IsSolved,
            string.Join("; ", solution.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return OperatingPointOverlay.Describe(solution, schematic.IdOf(designator));
    }
}
