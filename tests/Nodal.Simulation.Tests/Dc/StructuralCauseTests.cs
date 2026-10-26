using Nodal.Circuit.Schematics;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class StructuralCauseTests
{
    private static Schematic DiodeAcrossSource() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Diode, 6, 0, Circuits.FirstPinUp)
        .Wire(0, -2, 6, -2)
        .Wire(0, 2, 6, 2)
        .Set("V1", "voltage", 5);

    private static Schematic ReverseDiodeAcrossSource() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Diode, 6, 0, Circuits.FirstPinDown)
        .Wire(0, -2, 6, -2)
        .Wire(0, 2, 6, 2)
        .Set("V1", "voltage", 5);

    private static Schematic ParallelSourcesWithADiode() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.VoltageSource, 6, 0)
        .Add(Circuits.Ground, 6, 2)
        .Add(Circuits.Resistor, 12, 0, Circuits.FirstPinUp)
        .Add(Circuits.Diode, 12, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 12, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, -2, 12, -2)
        .Wire(12, 2, 12, 4)
        .Set("V1", "voltage", 5)
        .Set("V2", "voltage", 3)
        .Set("R1", "resistance", 1000);

    [Fact]
    public void ADiodeAcrossAnIdealSourceReportsTheUnboundedCurrent()
    {
        var solution = DiodeAcrossSource().Solve();

        Assert.False(solution.IsSolved);
        Assert.Equal(
            SimulationDiagnosticCode.UnboundedCurrent, solution.Diagnostics.Single().Code);
    }

    [Fact]
    public void TheUnboundedCurrentMessageNamesTheDiodeAndTheSource()
    {
        var solution = DiodeAcrossSource().Solve();

        Assert.Equal(
            "D1 is connected directly across the ideal source V1; its current would be "
            + "unbounded. Add a series resistance.",
            solution.Diagnostics.Single().Message);
    }

    [Fact]
    public void TheUnboundedCurrentDiagnosticNamesBothComponents()
    {
        var schematic = DiodeAcrossSource();
        var diagnostic = schematic.Solve().Diagnostics.Single();

        Assert.Contains(schematic.IdOf("D1"), diagnostic.Components);
        Assert.Contains(schematic.IdOf("V1"), diagnostic.Components);
    }

    [Fact]
    public void AReverseBiasedDiodeAcrossAnIdealSourceStillSolves()
    {
        var solution = ReverseDiodeAcrossSource().Solve();

        Assert.True(solution.IsSolved);
        Approximately.Equal(-5, solution.Of("D1").Voltage.Value, 1e-9);
    }

    [Fact]
    public void ASingularNonlinearCircuitReportsItsStructuralCause()
    {
        var solution = ParallelSourcesWithADiode().Solve();

        Assert.False(solution.IsSolved);
        Assert.Equal(SimulationDiagnosticCode.Singular, solution.Diagnostics.Single().Code);
        Assert.Equal(
            "The circuit has no unique DC solution: V1 and V2 are ideal voltage sources in "
            + "parallel across nets 0 and N1.",
            solution.Diagnostics.Single().Message);
    }
}
