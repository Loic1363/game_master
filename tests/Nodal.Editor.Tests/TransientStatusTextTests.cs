using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests;

public class TransientStatusTextTests
{
    private static Schematic Circuit() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, -2)
        .Add("ground", 8, 2)
        .Wire(0, -2, 4, -2)
        .Wire(8, -2, 8, 2);

    private static TransientResult Run(TransientOptions options) => TransientAnalysis.Run(
        NetlistCompiler.Compile(Circuit(), BaseContent.Catalog), BaseContent.Catalog, options);

    private static TransientResult Refused() => TransientAnalysis.Run(
        NetlistCompiler.Compile(Schematic.Empty.Add("resistor", 0, 0), BaseContent.Catalog),
        BaseContent.Catalog,
        new TransientOptions(0.01));

    [Fact]
    public void NothingRunYetIsStated() =>
        Assert.Equal("Transient: not run", TransientStatusText.Of(null));

    [Theory]
    [InlineData(0, "Transient: 0 %")]
    [InlineData(0.37, "Transient: 37 %")]
    [InlineData(0.999, "Transient: 99 %")]
    [InlineData(1, "Transient: 100 %")]
    [InlineData(-1, "Transient: 0 %")]
    [InlineData(2, "Transient: 100 %")]
    [InlineData(double.NaN, "Transient: 0 %")]
    public void ProgressIsShownAsAWholePercentage(double fraction, string expected) =>
        Assert.Equal(expected, TransientStatusText.Running(fraction));

    [Fact]
    public void ASolvedRunNamesTheTimeReachedAndThePointCount()
    {
        var result = Run(new TransientOptions(0.01));
        var text = TransientStatusText.Of(result);

        Assert.StartsWith("Transient: solved to 10 ms (", text, StringComparison.Ordinal);
        Assert.EndsWith($"({result.PointCount} points)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ARefusedRunIsStated()
    {
        var refused = Refused();

        Assert.False(refused.IsSolved);
        Assert.Equal("Transient: refused", TransientStatusText.Of(refused));
    }

    [Fact]
    public void AStoppedRunNamesTheFailureTime()
    {
        var stopped = TransientRuns.Stopped();

        Assert.True(stopped.IsPartial);
        Assert.Equal("Transient: stopped at 1 µs", TransientStatusText.Of(stopped));
    }
}
