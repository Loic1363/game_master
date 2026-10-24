using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class VoltageDividerTests
{
    private const double Supply = 10;
    private const double Upper = 1000;
    private const double Lower = 2200;
    private const double Middle = Supply * Lower / (Upper + Lower);
    private const double Current = Supply / (Upper + Lower);

    private static Schematic Divider() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Upper)
        .Set("R2", "resistance", Lower);

    [Fact]
    public void TenVoltsAcrossOneKiloOhmAndTwoPointTwoKiloOhmGivesSixPointEightSevenFiveVolts()
    {
        var schematic = Divider();
        var solution = schematic.Solve();

        Assert.True(solution.IsSolved);
        Approximately.Equal(Supply, solution.Volts(schematic.NetAt("R1", "1")));
        Approximately.Equal(Middle, solution.Volts(schematic.NetAt("R1", "2")));
        Approximately.Equal(0, solution.Volts("0"));
    }

    [Fact]
    public void TheSameCurrentOfThreePointOneTwoFiveMilliampereFlowsThroughBothResistors()
    {
        var solution = Divider().Solve();

        Approximately.Equal(Current, solution.Amperes("R1"));
        Approximately.Equal(Current, solution.Amperes("R2"));
    }

    [Fact]
    public void TheSourceCurrentIsNegativeBecauseItDelivers()
    {
        var solution = Divider().Solve();

        Approximately.Equal(-Current, solution.Amperes("V1"));
        Approximately.Equal(-Supply * Current, solution.Watts("V1"));
    }

    [Fact]
    public void EachResistorAbsorbsItsShareOfThePower()
    {
        var solution = Divider().Solve();

        Approximately.Equal(Current * Current * Upper, solution.Watts("R1"));
        Approximately.Equal(Current * Current * Lower, solution.Watts("R2"));
    }

    [Fact]
    public void TheDividerReportsNoDiagnostic()
    {
        Assert.Empty(Divider().Solve().Diagnostics);
    }
}
