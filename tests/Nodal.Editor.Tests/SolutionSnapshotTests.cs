using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests;

public class SolutionSnapshotTests
{
    private static Schematic Circuit() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, -2)
        .Add("ground", 8, 2)
        .Wire(0, -2, 4, -2)
        .Wire(8, -2, 8, 2);

    private static Netlist Netlist() =>
        NetlistCompiler.Compile(Circuit(), BaseContent.Catalog);

    private static OperatingPoint Dc() => DcAnalysis.Solve(Netlist(), BaseContent.Catalog);

    private static TransientResult Transient() => TransientAnalysis.Run(
        Netlist(), BaseContent.Catalog, new TransientOptions(0.01));

    private static TransientResult Refused() => TransientAnalysis.Run(
        NetlistCompiler.Compile(Schematic.Empty.Add("resistor", 0, 0), BaseContent.Catalog),
        BaseContent.Catalog,
        new TransientOptions(0.01));

    private static string NetOf(Schematic schematic, string designator, string pinName)
    {
        var connectivity = ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);
        var pin = new PinRef(schematic.IdOf(designator), pinName);

        Assert.True(connectivity.TryGetNetOfPin(pin, out var net));

        return net.Name;
    }

    [Fact]
    public void ADcSnapshotCarriesTheOperatingPoint()
    {
        var schematic = Circuit();
        var snapshot = SolutionSnapshot.FromDc(Dc());

        Assert.True(snapshot.IsSolved);
        Assert.Equal(0, snapshot.Time);
        Assert.True(snapshot.TryGetNetVoltage(NetOf(schematic, "V1", "plus"), out var voltage));
        Assert.Equal("5 V", voltage.ToString());
        Assert.True(snapshot.TryGetComponent(schematic.IdOf("R1"), out var resistor));
        Assert.Equal("R1", resistor.Designator);
    }

    [Fact]
    public void ATransientSnapshotMatchesTheDcSnapshotOnAResistiveCircuit()
    {
        var schematic = Circuit();
        var dc = SolutionSnapshot.FromDc(Dc());
        var result = Transient();
        var transient = SolutionSnapshot.FromTransient(result, result.FinalTime);
        var net = NetOf(schematic, "V1", "plus");

        Assert.True(transient.IsSolved);
        Assert.Equal(result.FinalTime, transient.Time);
        Assert.True(dc.TryGetNetVoltage(net, out var expected));
        Assert.True(transient.TryGetNetVoltage(net, out var actual));
        Assert.Equal(expected.Value, actual.Value, 9);

        Assert.True(dc.TryGetComponent(schematic.IdOf("R1"), out var expectedResistor));
        Assert.True(transient.TryGetComponent(schematic.IdOf("R1"), out var actualResistor));
        Assert.Equal(expectedResistor.Current.Value, actualResistor.Current.Value, 9);
        Assert.Equal(expectedResistor.Power.Value, actualResistor.Power.Value, 9);
    }

    [Fact]
    public void AnUnsolvedResultMakesAnUnsolvedSnapshot()
    {
        var snapshot = SolutionSnapshot.FromTransient(Refused(), 0);

        Assert.False(snapshot.IsSolved);
        Assert.False(snapshot.TryGetNetVoltage("N1", out _));
    }

    [Fact]
    public void TheOverlayDescribesATransientSnapshot()
    {
        var schematic = Circuit();
        var result = Transient();
        var snapshot = SolutionSnapshot.FromTransient(result, result.FinalTime);
        var described = OperatingPointOverlay.Describe(snapshot, schematic.IdOf("R1"));

        Assert.NotNull(described);
        Assert.StartsWith("R1: ", described, StringComparison.Ordinal);
        Assert.Contains("absorbed", described, StringComparison.Ordinal);
    }

    [Fact]
    public void TheOverlayLabelsATransientSnapshot()
    {
        var schematic = Circuit();
        var result = Transient();
        var connectivity = ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);

        var labels = OperatingPointOverlay.Labels(
            schematic,
            BaseContent.Catalog,
            connectivity,
            SolutionSnapshot.FromTransient(result, result.FinalTime));

        Assert.NotEmpty(labels);
        Assert.Contains(labels, label => label.Text == "5 V");
    }

    [Fact]
    public void ASnapshotReadsTheSeriesAtTheAskedTime()
    {
        var schematic = Circuit();
        var result = Transient();
        var early = SolutionSnapshot.FromTransient(result, 0);
        var late = SolutionSnapshot.FromTransient(result, result.FinalTime);

        Assert.Equal(0, early.Time);
        Assert.True(early.TryGetComponent(schematic.IdOf("R1"), out var first));
        Assert.True(late.TryGetComponent(schematic.IdOf("R1"), out var last));
        Assert.Equal(first.Current.Value, last.Current.Value, 9);
    }

    [Fact]
    public void ASnapshotOfAStoppedRunReadsTheAcceptedPrefix()
    {
        var stopped = TransientRuns.Stopped();
        var schematic = TransientRuns.PulsedRc();
        var snapshot = SolutionSnapshot.FromTransient(stopped, stopped.FinalTime);

        Assert.False(stopped.IsSolved);
        Assert.True(snapshot.IsSolved);
        Assert.True(snapshot.TryGetComponent(schematic.IdOf("C1"), out var capacitor));
        Assert.Equal("C1", capacitor.Designator);
    }

    [Fact]
    public void ASnapshotOfARefusedRunHasNoValues()
    {
        var snapshot = SolutionSnapshot.FromTransient(Refused(), 0);

        Assert.False(snapshot.IsSolved);
    }
}
