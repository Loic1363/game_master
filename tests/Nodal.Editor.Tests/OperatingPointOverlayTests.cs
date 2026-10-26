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

    [Fact]
    public void AHoveredNetIsDescribedWithItsNameAndVoltage()
    {
        var schematic = SourceAndResistor();

        Assert.Equal(
            "N1 = 5 V",
            OperatingPointOverlay.DescribeNet(
                Solve(schematic), NetOf(schematic, "V1", "plus")));
    }

    [Fact]
    public void AHoveredNetWithoutADcPathIsDescribedAsUndefined()
    {
        var schematic = Schematic.Empty
            .Add("voltage-source", 0, 0)
            .Add("ground", 0, 2)
            .Add("capacitor", 6, -2)
            .Wire(0, -2, 4, -2);

        var floating = NetOf(schematic, "C1", "2");

        Assert.Equal(
            $"{floating} = {OperatingPointOverlay.UndefinedText}",
            OperatingPointOverlay.DescribeNet(Solve(schematic), floating));
    }

    [Fact]
    public void ARefusedAnalysisDescribesNoNet()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        Assert.Null(OperatingPointOverlay.DescribeNet(Solve(schematic), "N1"));
    }

    [Fact]
    public void AVoltagePillBelowANanovoltReadsAsZero()
    {
        var schematic = SourceAndResistor().Set("V1", "voltage", 1e-12);

        Assert.Equal("0 V", LabelFor(schematic, NetOf(schematic, "V1", "plus")).Text);
    }

    [Fact]
    public void AHoveredNetBelowANanovoltReadsAsZero()
    {
        var schematic = SourceAndResistor().Set("V1", "voltage", 1e-12);
        var net = NetOf(schematic, "V1", "plus");

        Assert.Equal($"{net} = 0 V", OperatingPointOverlay.DescribeNet(Solve(schematic), net));
    }

    [Fact]
    public void AHoverTextBelowTheFloorsReadsAsZeroVoltsAndAmperes()
    {
        var schematic = SourceAndResistor()
            .Wire(8, -2, 8, 2)
            .Wire(0, 2, 8, 2)
            .Set("V1", "voltage", 1e-12)
            .Set("R1", "resistance", 1e6);

        var described = OperatingPointOverlay.Describe(
            Solve(schematic), schematic.IdOf("R1"));

        Assert.StartsWith("R1: 0 V, 0 A, ", described, StringComparison.Ordinal);
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
