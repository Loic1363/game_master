using Nodal.Circuit.Schematics;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class SingularTests
{
    private static Schematic ParallelSources() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.VoltageSource, 6, 0)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2);

    private static Schematic ParallelInductors() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Inductor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Inductor, 12, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Add(Circuits.Ground, 12, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Wire(6, 4, 12, 4);

    private static Schematic SourceLoop() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.VoltageSource, 6, -4)
        .Add(Circuits.VoltageSource, 12, -2)
        .Add(Circuits.Ground, 12, 0)
        .Wire(0, -2, 6, -2)
        .Wire(6, -6, 12, -6)
        .Wire(12, -6, 12, -4);

    [Fact]
    public void TwoIdealSourcesInParallelHaveNoUniqueSolution()
    {
        var solution = ParallelSources().Solve();

        Assert.False(solution.IsSolved);
        Assert.Empty(solution.NetVoltages);
        Assert.Equal(SimulationDiagnosticCode.Singular, solution.Diagnostics.Single().Code);
    }

    [Fact]
    public void TheParallelSourceMessageNamesBothSourcesAndTheirNets()
    {
        var schematic = ParallelSources();
        var shared = schematic.NetAt("V1", "plus");

        Assert.Equal(
            "The circuit has no unique DC solution: V1 and V2 are ideal voltage sources in "
            + $"parallel across nets 0 and {shared}.",
            schematic.Solve().Diagnostics.Single().Message);
    }

    [Fact]
    public void TheParallelSourceDiagnosticCarriesBothComponents()
    {
        var schematic = ParallelSources();
        var diagnostic = schematic.Solve().Diagnostics.Single();

        Assert.Equal(
            [schematic.IdOf("V1"), schematic.IdOf("V2")], diagnostic.Components);
    }

    [Fact]
    public void TwoIdealInductorsInParallelAreNamedAsInductors()
    {
        Assert.Contains(
            "L1 and L2 are ideal inductors in parallel",
            ParallelInductors().Solve().Diagnostics.Single().Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ALoopOfIdealSourcesIsNamedAsALoop()
    {
        var solution = SourceLoop().Solve();

        Assert.False(solution.IsSolved);
        Assert.Equal(
            "The circuit has no unique DC solution: V1, V2 and V3 form a loop of ideal "
            + "voltage sources.",
            solution.Diagnostics.Single().Message);
    }

    [Fact]
    public void ASeriesResistanceOnOneSourceRemovesTheDegeneracy()
    {
        var solution = ParallelSources().Set("V2", "series-resistance", 100).Solve();

        Assert.True(solution.IsSolved);
    }
}
