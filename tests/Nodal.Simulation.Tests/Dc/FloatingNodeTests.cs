using Nodal.Circuit.Schematics;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class FloatingNodeTests
{
    private static Schematic Circuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Capacitor, 6, 0, Circuits.FirstPinUp)
        .Wire(0, -2, 6, -2)
        .Set("V1", "voltage", 10);

    [Fact]
    public void ANodeBehindACapacitorIsReportedAsHavingNoDcPath()
    {
        var schematic = Circuit();
        var floating = schematic.NetAt("C1", "2");
        var solution = schematic.Solve();

        Assert.True(solution.IsSolved);
        Assert.Equal(
            [SimulationDiagnostic.NoDcPath(floating)],
            solution.Diagnostics);
        Assert.True(solution.IsUndefined(floating));
    }

    [Fact]
    public void TheFloatingNodeHasNoVoltageInTheSolution()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Assert.False(solution.TryGetNetVoltage(schematic.NetAt("C1", "2"), out _));
        Assert.DoesNotContain(schematic.NetAt("C1", "2"), solution.NetVoltages.Keys);
    }

    [Fact]
    public void TheDrivenNodeKeepsItsExactVoltage()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Approximately.Equal(10, solution.Volts(schematic.NetAt("C1", "1")));
    }

    [Fact]
    public void TheMessageNamesTheNetAndItsUndefinedVoltage()
    {
        var schematic = Circuit();
        var floating = schematic.NetAt("C1", "2");

        Assert.Equal(
            $"{floating} has no DC path to ground; its voltage is undefined.",
            schematic.Solve().Diagnostics.Single().Message);
    }

    [Fact]
    public void AGroundedCircuitReportsNoUndefinedNet()
    {
        var schematic = Circuit()
            .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 8)
            .Wire(6, 2, 6, 4);

        Assert.Empty(schematic.Solve().UndefinedNets);
    }
}
