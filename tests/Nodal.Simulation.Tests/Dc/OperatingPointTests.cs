using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class OperatingPointTests
{
    private static Schematic DemoCircuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Resistor, 6, -4)
        .Add(Circuits.Capacitor, 8, 0, new Circuit.Geometry.Orientation(
            Circuit.Geometry.Rotation.R90, false))
        .Add(Circuits.Ground, 0, 4)
        .Add(Circuits.Ground, 8, 4)
        .Wire(0, -2, 0, -4)
        .Wire(0, -4, 4, -4)
        .Wire(8, -4, 8, -2)
        .Wire(0, 2, 8, 2)
        .Wire(0, 2, 0, 4)
        .Wire(8, 2, 8, 4);

    [Fact]
    public void TheDemoCircuitHoldsFiveVoltsOnBothNodesAndCarriesNoCurrent()
    {
        var solution = DemoCircuit().Solve();

        Assert.True(solution.IsSolved);
        Assert.Equal(3, solution.NetVoltages.Count);
        Approximately.Equal(5, solution.Volts("N1"));
        Approximately.Equal(5, solution.Volts("N2"));
        Approximately.Zero(solution.Volts("0"), 0);
        Approximately.Zero(solution.Amperes("R1"), 1e-15);
        Approximately.Zero(solution.Amperes("V1"), 1e-15);
        Approximately.Zero(solution.Amperes("C1"), 1e-15);
    }

    [Fact]
    public void TheDebugTextListsEveryNetAndComponent()
    {
        Assert.Equal(
            """
            net 0 = 0 V
            net N1 = 5 V
            net N2 = 5 V
            V1 V=5 V I=0 A P=0 W
            R1 V=0 V I=0 A P=0 W
            C1 V=5 V I=0 A P=0 W
            """,
            DemoCircuit().Solve().ToDebugText());
    }

    [Fact]
    public void TheDebugTextOfARefusedAnalysisStartsWithRefused()
    {
        var text = Schematic.Empty.Solve().ToDebugText();

        Assert.StartsWith("refused", text, StringComparison.Ordinal);
        Assert.Contains("NoGround", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentsAreLookedUpByIdentifier()
    {
        var schematic = DemoCircuit();
        var solution = schematic.Solve();

        Assert.True(solution.TryGetComponent(schematic.IdOf("R1"), out var resistor));
        Assert.Equal("R1", resistor.Designator);
        Assert.Equal(Unit.Ampere, resistor.Current.Unit);
        Assert.Equal(Unit.Watt, resistor.Power.Unit);
        Assert.Equal(Unit.Volt, resistor.Voltage.Unit);
    }

    [Fact]
    public void AGroundSymbolHasNoResult()
    {
        var schematic = DemoCircuit();

        Assert.False(schematic.Solve().TryGetComponent(schematic.IdOf("GND1"), out _));
    }

    [Fact]
    public void AnUnknownNetOrComponentIsReportedAsMissing()
    {
        var solution = DemoCircuit().Solve();

        Assert.False(solution.TryGetNetVoltage("N9", out _));
        Assert.False(solution.IsUndefined("N1"));
        Assert.Empty(solution.RatingWarnings);
    }
}
