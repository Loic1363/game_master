using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Netlists;
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
    public void ADiodeIsSupportedAndNoLongerRefused()
    {
        var solution = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Diode, 6, 6, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 8)
            .Wire(0, -2, 6, -2)
            .Wire(6, 2, 6, 4)
            .Set("V1", "voltage", 5)
            .Set("R1", "resistance", 1000)
            .Solve();

        Assert.True(solution.IsSolved);
        Assert.DoesNotContain(
            SimulationDiagnosticCode.UnsupportedComponent,
            solution.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Theory]
    [InlineData("nmos", "M1")]
    [InlineData("pmos", "M1")]
    [InlineData("npn", "Q1")]
    [InlineData("pnp", "Q1")]
    public void NoTransistorIsRefusedAsUnsupported(string definitionId, string designator)
    {
        var solution = TransistorCircuits.BiasBox(definitionId).Solve();

        Assert.True(solution.IsSolved);
        Assert.DoesNotContain(
            SimulationDiagnosticCode.UnsupportedComponent,
            solution.Diagnostics.Select(diagnostic => diagnostic.Code));

        Assert.NotNull(solution.Of(designator));
    }

    [Fact]
    public void EachComponentTheSimulatorDoesNotModelGetsItsOwnDiagnostic()
    {
        var solution = DcAnalysis.Solve(
            UnmodelledComponents.Netlist(
                UnmodelledComponents.Transformer, UnmodelledComponents.Relay),
            UnmodelledComponents.Catalog);

        var unsupported = solution.Diagnostics
            .Where(diagnostic => diagnostic.Code == SimulationDiagnosticCode.UnsupportedComponent)
            .ToArray();

        Assert.False(solution.IsSolved);
        Assert.Equal(2, unsupported.Length);
        Assert.Contains(
            "X1 (Transformer) is not supported by the simulator yet.",
            unsupported.Select(diagnostic => diagnostic.Message));
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
            ImmutableSortedDictionary.Create<string, ParameterValue>(StringComparer.Ordinal));

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
