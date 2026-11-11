using System.Globalization;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Schedules;
using Nodal.Simulation.Spice;
using Nodal.Simulation.Tests.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Spice;

public class ControlExportTests
{
    [Fact]
    public void AVoltageControlledVoltageSourceKeepsItsNodeOrder() =>
        Assert.Contains(
            "E1 N2 0 N1 0 10",
            Lines(ControlCircuits.VcvsAmplifier(0.5, 10, 1000)));

    [Fact]
    public void AVoltageControlledCurrentSourceFlipsItsOutputNodes() =>
        Assert.Contains(
            "G1 0 N2 N1 0 0.001",
            Lines(ControlCircuits.VccsLoad(1, 1e-3, 2000)));

    [Fact]
    public void ACurrentControlledCurrentSourceReadsAHiddenSenseSource()
    {
        var lines = Lines(ControlCircuits.CccsLoad(1, 1000, 10, 100));

        Assert.Contains("F1 0 N3 VF1_sense 10", lines);
        Assert.Contains("VF1_sense N2 0 0", lines);
    }

    [Fact]
    public void ACurrentControlledVoltageSourceReadsAHiddenSenseSource()
    {
        var lines = Lines(ControlCircuits.CcvsLoad(1, 1000, 2000, 1000));

        Assert.Contains("H1 N3 0 VH1_sense 2000", lines);
        Assert.Contains("VH1_sense N2 0 0", lines);
    }

    [Fact]
    public void ASwitchCarriesItsOwnModel()
    {
        var lines = Lines(ControlCircuits.SwitchLoad(5, 5, 1000));

        Assert.Contains("S1 N2 N3 N1 0 NODAL_SW_S1", lines);
        Assert.Contains(".model NODAL_SW_S1 SW(VT=2.5 VH=0 RON=1 ROFF=1000000000)", lines);
    }

    [Fact]
    public void APushButtonBecomesASwitchDrivenByAHiddenControlSource()
    {
        var lines = Lines(Button("1m to 3m"));

        Assert.Contains("SW1 N1 N2 SW1_ctrl 0 NODAL_SW_SW1", lines);
        Assert.Contains(".model NODAL_SW_SW1 SW(VT=0.5 VH=0 RON=0.01 ROFF=1000000000)", lines);
    }

    [Fact]
    public void EachScheduledEdgeIsCentredOnItsInstant() =>
        Assert.Contains(
            "VSW1_ctrl SW1_ctrl 0 PWL(0 0 0.0009999995 0 0.0010000005 1 0.0029999995 1 "
            + "0.0030000005000000002 0)",
            Lines(Button("1m to 3m")));

    [Fact]
    public void AReleasedButtonHoldsZeroVolts() =>
        Assert.Contains("VSW1_ctrl SW1_ctrl 0 PWL(0 0)", Lines(Button("released")));

    [Fact]
    public void APressedButtonStartsAtOneVoltWithoutALeadingEdge() =>
        Assert.Contains("VSW1_ctrl SW1_ctrl 0 PWL(0 1)", Lines(Button("pressed")));

    [Fact]
    public void APressStartingInsideHalfAnEdgeStartsAtOneVoltWithoutALeadingEdge()
    {
        var points = PointsOf(Button("0.4n to 1.5n"));

        Assert.Equal((0, 1), points[0]);
        Assert.Equal(3, points.Length);
        Assert.Equal(1e-9, points[1].Time, 1e-18);
        Assert.Equal(1.0, points[1].Level);
        Assert.Equal(2e-9, points[2].Time, 1e-18);
        Assert.Equal(0.0, points[2].Level);
    }

    [Fact]
    public void InstantsCloserThanAnEdgeShareTheGap()
    {
        var points = PointsOf(Button("1m to 1.0000004m"));

        Assert.Equal(5, points.Length);
        Assert.Equal(0.2e-9, points[2].Time - points[1].Time, 1e-18);
        Assert.Equal(0.2e-9, points[4].Time - points[3].Time, 1e-18);
        Assert.True(
            points[2].Time < points[3].Time,
            "the falling edge must start after the rising edge ends");

        Assert.Equal([0.0, 0.0, 1.0, 1.0, 0.0], points.Select(point => point.Level));
    }

    [Fact]
    public void HiddenNamesAreADesignatorAndASuffixWhileDesignatorsCarryNoUnderscore()
    {
        var schematic = ControlCircuits.CccsLoad(1, 1000, 10, 100);

        Assert.All(
            schematic.Components.Values,
            component => Assert.DoesNotContain(
                "_", component.Designator, StringComparison.Ordinal));

        Assert.Equal(
            ["VF1_sense"],
            Lines(schematic)
                .SelectMany(line => line.Split(' '))
                .Where(token => token.Contains('_', StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal));
    }

    private static (double Time, double Level)[] PointsOf(Schematic schematic)
    {
        var line = Lines(schematic).Single(
            candidate => candidate.StartsWith("VSW1_ctrl", StringComparison.Ordinal));

        var open = line.IndexOf('(', StringComparison.Ordinal);

        var numbers = line[(open + 1)..^1]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => double.Parse(token, CultureInfo.InvariantCulture))
            .ToArray();

        return
        [
            .. Enumerable.Range(0, numbers.Length / 2).Select(
                index => (numbers[index * 2], numbers[(index * 2) + 1])),
        ];
    }

    private static Schematic Button(string presses)
    {
        Assert.True(PressScheduleText.TryParse(presses, out var schedule, out _), presses);

        return ControlCircuits.PushButtonLoad(schedule!, 5, 1000);
    }

    private static string[] Lines(Schematic schematic) =>
        SpiceExporter.Export(
                NetlistCompiler.Compile(schematic, BaseCatalog.Catalog), BaseCatalog.Catalog)
            .Split('\n');
}
