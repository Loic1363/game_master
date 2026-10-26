using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Editor.Tests;

public class DcStatusTextTests
{
    private static Schematic Linear() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, -2)
        .Add("ground", 8, 2)
        .Wire(0, -2, 4, -2)
        .Wire(8, -2, 8, 2);

    private static Schematic WithADiode() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, -2)
        .Add("diode", 14, -2)
        .Add("ground", 16, 2)
        .Wire(0, -2, 4, -2)
        .Wire(8, -2, 12, -2)
        .Wire(16, -2, 16, 2);

    [Fact]
    public void WithoutASolutionTheStatusSaysTheAnalysisWasNotRun()
    {
        Assert.Equal(DcStatusText.NotRunText, DcStatusText.Of(null));
    }

    [Fact]
    public void ARefusedAnalysisAlsoSaysTheAnalysisWasNotRun()
    {
        Assert.Equal(DcStatusText.NotRunText, DcStatusText.Of(Solve(Schematic.Empty)));
    }

    [Fact]
    public void ALinearCircuitReportsASingleIteration()
    {
        Assert.Equal("DC: solved (1 iteration)", DcStatusText.Of(Solve(Linear())));
    }

    [Fact]
    public void ANonlinearCircuitReportsItsIterationCount()
    {
        var solution = Solve(WithADiode());

        Assert.Equal(
            $"DC: solved ({solution.Convergence!.Iterations} iterations)",
            DcStatusText.Of(solution));
    }

    [Fact]
    public void ASteppedSolutionNamesTheStrategy()
    {
        var solution = Solve(WithADiode(), new DcOptions { MaxIterations = 3 });

        Assert.True(solution.IsSolved);
        Assert.Equal(
            $"DC: solved by source stepping ({solution.Convergence!.Iterations} iterations)",
            DcStatusText.Of(solution));
    }

    private static OperatingPoint Solve(Schematic schematic, DcOptions? options = null) =>
        DcAnalysis.Solve(
            NetlistCompiler.Compile(schematic, BaseContent.Catalog),
            BaseContent.Catalog,
            options);
}
