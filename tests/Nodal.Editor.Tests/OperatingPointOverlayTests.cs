using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Foundation.Geometry;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Editor.Tests;

public class OperatingPointOverlayTests
{
    private static Schematic SourceAndResistor() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, -2)
        .Wire(0, -2, 4, -2);

    private static Schematic WithABranch() => SourceAndResistor()
        .Add("resistor", 6, 6)
        .Wire(3, -2, 3, 6)
        .Wire(3, 6, 4, 6);

    [Fact]
    public void ANetWithoutAJunctionIsLabelledAtTheMidpointOfItsFirstWire()
    {
        var schematic = SourceAndResistor();
        var label = LabelFor(schematic, NetOf(schematic, "V1", "plus"));

        Assert.Equal(new GridVector(2, -2), label.Position);
        Assert.Equal("5 V", label.Text);
    }

    [Fact]
    public void ANetWithAJunctionIsLabelledAtThatJunction()
    {
        var schematic = WithABranch();
        var label = LabelFor(schematic, NetOf(schematic, "V1", "plus"));

        Assert.Equal(new GridVector(3, -2), label.Position);
    }

    [Fact]
    public void ANetWithoutAWireIsLabelledAtItsFirstPin()
    {
        var schematic = SourceAndResistor();
        var label = LabelFor(schematic, NetOf(schematic, "R1", "2"));

        Assert.Equal(new GridVector(8, -2), label.Position);
    }

    [Fact]
    public void TheGroundNetIsNotLabelled()
    {
        var schematic = SourceAndResistor();
        var ground = NetOf(schematic, "GND1", "gnd");

        Assert.DoesNotContain(ground, LabelsOf(schematic).Select(label => label.NetName));
    }

    [Fact]
    public void ANetWithoutADcPathReadsAsUndefined()
    {
        var schematic = Schematic.Empty
            .Add("voltage-source", 0, 0)
            .Add("ground", 0, 2)
            .Add("capacitor", 6, -2)
            .Wire(0, -2, 4, -2);

        Assert.Equal(
            OperatingPointOverlay.UndefinedText,
            LabelFor(schematic, NetOf(schematic, "C1", "2")).Text);
    }

    [Fact]
    public void ARefusedAnalysisHasNoLabels()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);
        var connectivity = ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);
        var refused = DcAnalysis.Solve(
            NetlistCompiler.Compile(schematic, BaseContent.Catalog), BaseContent.Catalog);

        Assert.False(refused.IsSolved);
        Assert.Empty(
            OperatingPointOverlay.Labels(schematic, BaseContent.Catalog, connectivity, refused));
    }

    [Fact]
    public void EveryNonGroundNetIsLabelledExactlyOnce()
    {
        var labels = LabelsOf(WithABranch());

        Assert.Equal(3, labels.Length);
        Assert.Equal(
            labels.Select(label => label.NetName).Distinct().Count(), labels.Length);
    }

    [Fact]
    public void AResistorIsDescribedAsAbsorbing()
    {
        var schematic = SourceAndResistor().Wire(8, -2, 8, 2).Wire(0, 2, 8, 2);

        Assert.Equal(
            "R1: 5 V, 5 mA, 25 mW absorbed",
            OperatingPointOverlay.Describe(Solve(schematic), schematic.IdOf("R1")));
    }

    [Fact]
    public void ASourceIsDescribedAsDelivering()
    {
        var schematic = SourceAndResistor().Wire(8, -2, 8, 2).Wire(0, 2, 8, 2);

        Assert.Equal(
            "V1: 5 V, 5 mA, 25 mW delivered",
            OperatingPointOverlay.Describe(Solve(schematic), schematic.IdOf("V1")));
    }

    [Fact]
    public void AComponentWithoutAResultIsNotDescribed()
    {
        var schematic = SourceAndResistor();

        Assert.Null(OperatingPointOverlay.Describe(Solve(schematic), schematic.IdOf("GND1")));
    }

    private static NetVoltageLabel LabelFor(Schematic schematic, string netName) =>
        LabelsOf(schematic).Single(
            label => string.Equals(label.NetName, netName, StringComparison.Ordinal));

    private static ImmutableArray<NetVoltageLabel> LabelsOf(Schematic schematic) =>
        OperatingPointOverlay.Labels(
            schematic,
            BaseContent.Catalog,
            ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog),
            Solve(schematic));

    private static OperatingPoint Solve(Schematic schematic) => DcAnalysis.Solve(
        NetlistCompiler.Compile(schematic, BaseContent.Catalog), BaseContent.Catalog);

    private static string NetOf(Schematic schematic, string designator, string pinName)
    {
        var connectivity = ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);
        var pin = new PinRef(schematic.IdOf(designator), pinName);

        return connectivity.TryGetNetOfPin(pin, out var net)
            ? net.Name
            : throw new InvalidOperationException($"The pin {pin} belongs to no net.");
    }
}
