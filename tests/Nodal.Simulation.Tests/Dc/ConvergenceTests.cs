using Nodal.Circuit.Schematics;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class ConvergenceTests
{
    private const double Supply = 5;
    private const double Series = 1000;

    private static Schematic Diode() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Diode, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Series);

    private static Schematic Linear() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Series);

    [Fact]
    public void ALinearCircuitIsSolvedDirectlyInASingleIteration()
    {
        var solution = Linear().Solve();

        Assert.True(solution.IsSolved);
        Assert.Equal(DcSolveStrategy.Direct, solution.Convergence?.Strategy);
        Assert.Equal(1, solution.Convergence?.Iterations);
    }

    [Fact]
    public void ANonlinearCircuitIsSolvedByNewtonRaphson()
    {
        var solution = Diode().Solve();

        Assert.True(solution.IsSolved);
        Assert.Equal(DcSolveStrategy.NewtonRaphson, solution.Convergence?.Strategy);
        Assert.InRange(solution.Convergence!.Iterations, 2, DcOptions.Default.MaxIterations);
    }

    [Fact]
    public void ARefusedAnalysisReportsNoConvergence()
    {
        Assert.Null(Schematic.Empty.Solve().Convergence);
    }

    [Fact]
    public void WhenPlainNewtonRaphsonRunsOutOfIterationsSteppingTakesOver()
    {
        var solution = Solve(Diode(), new DcOptions { MaxIterations = 3 });

        Assert.True(solution.IsSolved);
        Assert.Contains(
            solution.Convergence!.Strategy,
            new[] { DcSolveStrategy.GminStepping, DcSolveStrategy.SourceStepping });

        Approximately.Equal(
            solution.Of("D1").Voltage.Value, Diode().Solve().Of("D1").Voltage.Value, 1e-6);
    }

    [Fact]
    public void TheSteppingStrategyCountsEveryIterationItSpent()
    {
        var stepped = Solve(Diode(), new DcOptions { MaxIterations = 3 });

        Assert.True(stepped.Convergence!.Iterations > 3);
    }

    [Fact]
    public void WithoutSteppingAnUnconvergedCircuitIsRefused()
    {
        var solution = Solve(
            Diode(), new DcOptions { MaxIterations = 1, StepOnFailure = false });

        Assert.False(solution.IsSolved);
        Assert.Equal(
            SimulationDiagnosticCode.NonConvergence, solution.Diagnostics.Single().Code);
    }

    [Fact]
    public void TheNonConvergenceMessageNamesTheIterationCount()
    {
        var solution = Solve(
            Diode(), new DcOptions { MaxIterations = 1, StepOnFailure = false });

        Assert.Equal(
            "The DC analysis did not converge after 1 iterations. The circuit may have no "
            + "stable operating point, or a value may be unrealistic.",
            solution.Diagnostics.Single().Message);
    }

    [Fact]
    public void ALinearCircuitIgnoresTheSteppingFlagEntirely()
    {
        var solution = Solve(
            Linear(), new DcOptions { MaxIterations = 1, StepOnFailure = false });

        Assert.True(solution.IsSolved);
        Assert.Equal(DcSolveStrategy.Direct, solution.Convergence?.Strategy);
    }

    [Fact]
    public void TheDefaultOptionsCarryTheSpiceTolerances()
    {
        var options = DcOptions.Default;

        Assert.Equal(1e-3, options.RelTol);
        Assert.Equal(1e-6, options.VnTol);
        Assert.Equal(1e-12, options.AbsTol);
        Assert.Equal(100, options.MaxIterations);
        Assert.Equal(1e-12, options.Gmin);
        Assert.True(options.StepOnFailure);
    }

    private static OperatingPoint Solve(Schematic schematic, DcOptions options) =>
        DcAnalysis.Solve(schematic.Compile(), BaseCatalog.Catalog, options);
}
