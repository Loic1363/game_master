using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Tests.Connectivity;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Equality;

public class StructuralRecordEqualityTests
{
    [Fact]
    public void NetsWithEqualContentsAreEqual()
    {
        Assert.Equal(Net("N1"), Net("N1"));
        Assert.Equal(Net("N1").GetHashCode(), Net("N1").GetHashCode());
    }

    [Fact]
    public void NetsDifferingInContentsOrOrderAreNotEqual()
    {
        var reordered = new Net(
            "N1", [new PinRef(new ComponentId(2), "1"), new PinRef(ComponentId.First, "1")], [WireId.First]);

        Assert.NotEqual(Net("N1"), reordered);
        Assert.NotEqual(Net("N1"), Net("N2"));
        Assert.NotEqual(Net("N1"), new Net("N1", Net("N1").Pins, [new WireId(7)]));
    }

    [Fact]
    public void NetlistElementsWithEqualContentsAreEqual()
    {
        Assert.Equal(Element("R1"), Element("R1"));
        Assert.Equal(Element("R1").GetHashCode(), Element("R1").GetHashCode());
        Assert.NotEqual(Element("R1"), Element("R2"));
    }

    [Fact]
    public void NetlistElementsDifferingInParametersOrPinsAreNotEqual()
    {
        var otherParameters = Element("R1") with
        {
            Parameters = Parameters(2e3),
        };

        var otherPins = Element("R1") with
        {
            Pins = [new PinConnection("1", "N1"), new PinConnection("2", "0")],
        };

        Assert.NotEqual(Element("R1"), otherParameters);
        Assert.NotEqual(Element("R1"), otherPins);
    }

    [Fact]
    public void DiagnosticsWithEqualContentsAreEqual()
    {
        var pin = new PinRef(ComponentId.First, "1");
        var first = ConnectivityDiagnostic.FloatingPin(pin, "N1", new GridPoint(2, 0));
        var second = ConnectivityDiagnostic.FloatingPin(pin, "N1", new GridPoint(2, 0));
        var other = ConnectivityDiagnostic.FloatingPin(pin, "N1", new GridPoint(3, 0));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void ComponentInstancesWithEqualParametersAreEqual()
    {
        Assert.Equal(Instance(1e3), Instance(1e3));
        Assert.Equal(Instance(1e3).GetHashCode(), Instance(1e3).GetHashCode());
        Assert.NotEqual(Instance(1e3), Instance(2e3));
    }

    [Fact]
    public void TwoAnalysesOfTheSameSchematicProduceEqualNetsAndDiagnostics()
    {
        var schematic = Divider();

        var first = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);
        var second = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal(first.Nets, second.Nets);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
    }

    [Fact]
    public void TwoCompilationsOfTheSameSchematicProduceEqualElements()
    {
        var schematic = Divider();

        var first = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);
        var second = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        Assert.Equal(first.Elements, second.Elements);
    }

    private static Schematic Divider() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("resistor", 6, -4)
        .Add("ground", 0, 4)
        .Wire(0, -2, 0, -4)
        .Wire(0, -4, 4, -4)
        .Wire(0, 2, 0, 4);

    private static Net Net(string name) => new(
        name,
        [new PinRef(ComponentId.First, "1"), new PinRef(new ComponentId(2), "1")],
        [WireId.First]);

    private static NetlistElement Element(string designator) => new(
        ComponentId.First,
        designator,
        "resistor",
        Parameters(1e3),
        [new PinConnection("1", "N1"), new PinConnection("2", "N2")]);

    private static ComponentInstance Instance(double resistance) => new(
        ComponentId.First,
        "resistor",
        "R1",
        GridPoint.Origin,
        Orientation.Default,
        Parameters(resistance));

    private static ImmutableSortedDictionary<string, ParameterValue> Parameters(double resistance) =>
        ImmutableSortedDictionary
            .Create<string, ParameterValue>(StringComparer.Ordinal)
            .Add("resistance", new Quantity(resistance, Unit.Ohm));
}
