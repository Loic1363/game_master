using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class RefusalTests
{
    [Fact]
    public void ACircuitWithoutGroundIsRefused()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Wire(0, -2, 6, -2)
            .Wire(0, 2, 6, 2);

        var solution = schematic.Solve();

        Assert.False(solution.IsSolved);
        Assert.Equal(SimulationDiagnosticCode.NoGround, solution.Diagnostics.Single().Code);
        Assert.Equal(
            "The circuit has no net named 0, so the DC analysis has no voltage reference.",
            solution.Diagnostics.Single().Message);
    }

    [Fact]
    public void AnEmptySchematicIsRefusedForTheSameReason()
    {
        var solution = Schematic.Empty.Solve();

        Assert.False(solution.IsSolved);
        Assert.Equal(SimulationDiagnosticCode.NoGround, solution.Diagnostics.Single().Code);
    }

    [Fact]
    public void ADiodeIsNotSupportedYet()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add("diode", 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2);

        var solution = schematic.Solve();

        Assert.False(solution.IsSolved);
        Assert.Equal(
            "D1 (Diode) is not supported by the simulator yet.",
            solution.Diagnostics.Single().Message);
        Assert.Equal([schematic.IdOf("D1")], solution.Diagnostics.Single().Components);
    }

    [Theory]
    [InlineData("nmos", "M1", "N-channel MOSFET")]
    [InlineData("pmos", "M1", "P-channel MOSFET")]
    [InlineData("npn", "Q1", "NPN transistor")]
    [InlineData("pnp", "Q1", "PNP transistor")]
    public void EveryTransistorIsRefusedWithItsOwnDiagnostic(
        string definitionId, string designator, string name)
    {
        var schematic = Schematic.Empty
            .Add(Circuits.Ground, 0, 0)
            .Add(definitionId, 6, 0);

        var solution = schematic.Solve();

        Assert.False(solution.IsSolved);
        Assert.Contains(
            $"{designator} ({name}) is not supported by the simulator yet.",
            solution.Diagnostics.Select(diagnostic => diagnostic.Message));
    }

    [Fact]
    public void EachUnsupportedComponentGetsItsOwnDiagnostic()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.Ground, 0, 0)
            .Add("diode", 6, 0)
            .Add("diode", 12, 0);

        var unsupported = schematic.Solve().Diagnostics
            .Where(diagnostic => diagnostic.Code == SimulationDiagnosticCode.UnsupportedComponent)
            .ToArray();

        Assert.Equal(2, unsupported.Length);
    }

    [Fact]
    public void AConnectivityErrorRefusesTheAnalysis()
    {
        var unknown = new ComponentInstance(
            ComponentId.First,
            "flux-capacitor",
            "X1",
            GridPoint.Origin,
            Orientation.Default,
            ImmutableSortedDictionary.Create<string, Quantity>(StringComparer.Ordinal));

        var schematic = Schematic.Create(
            [unknown], [], new ComponentId(ComponentId.First.Value + 1), WireId.First);

        var solution = DcAnalysis.Solve(schematic.Compile(), BaseCatalog.Catalog);

        Assert.False(solution.IsSolved);
        Assert.Contains(
            solution.Diagnostics,
            diagnostic => diagnostic.Code == SimulationDiagnosticCode.Connectivity);
    }

    [Fact]
    public void ARefusedAnalysisHasNoValues()
    {
        var solution = Schematic.Empty.Solve();

        Assert.Empty(solution.NetVoltages);
        Assert.Empty(solution.Components);
        Assert.True(solution.Errors.Length > 0);
        Assert.Empty(solution.Warnings);
    }
}
