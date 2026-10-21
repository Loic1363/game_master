using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Xunit;
using static Nodal.Circuit.Tests.Connectivity.ConnectivityTests;

namespace Nodal.Circuit.Tests.Connectivity;

public class NetNamingTests
{
    [Fact]
    public void PinNetsAreNumberedBySmallestPinReference()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 10, 0)
            .Wire(2, 0, 8, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal("N1", NetOf(result, Pin(1, "1")));
        Assert.Equal("N2", NetOf(result, Pin(1, "2")));
        Assert.Equal("N3", NetOf(result, Pin(2, "2")));
    }

    [Fact]
    public void TheSmallestPinReferenceUsesDefinitionPinOrderNotPinName()
    {
        var schematic = Schematic.Empty.Add("npn", 0, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal("N1", NetOf(result, Pin(1, "collector")));
        Assert.Equal("N2", NetOf(result, Pin(1, "base")));
        Assert.Equal("N3", NetOf(result, Pin(1, "emitter")));
    }

    [Fact]
    public void PinNetsAreNumberedBeforeWireOnlyNets()
    {
        var schematic = Schematic.Empty
            .Wire(0, 50, 6, 50)
            .Add("resistor", 0, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal("N1", NetOf(result, Pin(1, "1")));
        Assert.Equal("N2", NetOf(result, Pin(1, "2")));
        Assert.True(result.TryGetNetOfWire(WireId.First, out var wireNet));
        Assert.Equal("N3", wireNet.Name);
    }

    [Fact]
    public void WireOnlyNetsAreNumberedBySmallestWireId()
    {
        var schematic = Schematic.Empty
            .Wire(0, 20, 6, 20)
            .Wire(0, 10, 6, 10)
            .Wire(0, 0, 6, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal(["N1", "N2", "N3"], result.NetNames);
        Assert.True(result.TryGetNetOfWire(new WireId(1), out var first));
        Assert.Equal("N1", first.Name);
        Assert.True(result.TryGetNetOfWire(new WireId(3), out var third));
        Assert.Equal("N3", third.Name);
    }

    [Fact]
    public void TheGlobalNetKeepsItsNameAndTheOthersAreNumberedContiguously()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("ground", 2, 0)
            .Add("resistor", 20, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal(["0", "N1", "N2", "N3"], result.NetNames);
        Assert.Equal("0", NetOf(result, Pin(1, "2")));
        Assert.Equal("N1", NetOf(result, Pin(1, "1")));
        Assert.Equal("N2", NetOf(result, Pin(3, "1")));
        Assert.Equal("N3", NetOf(result, Pin(3, "2")));
    }

    [Fact]
    public void TheGroundNetComesFirstInTheNetList()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("ground", 20, 20);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal("0", result.Nets[0].Name);
    }

    [Fact]
    public void BuildingTheSameCircuitInAnotherCommandOrderGivesTheSameNets()
    {
        var wireLast = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 10, 0)
            .Wire(2, 0, 8, 0);

        var wireFirst = Schematic.Empty
            .Wire(2, 0, 8, 0)
            .Add("resistor", 0, 0)
            .Add("resistor", 10, 0);

        var left = ConnectivityAnalyzer.Analyze(wireLast, BaseCatalog.Catalog);
        var right = ConnectivityAnalyzer.Analyze(wireFirst, BaseCatalog.Catalog);

        Assert.Equal(left.NetNames, right.NetNames);
        Assert.Equal(Describe(left), Describe(right));
        Assert.Equal(left.Junctions, right.Junctions);
    }

    [Fact]
    public void AnalyzingTheSameSchematicTwiceGivesTheSameResult()
    {
        var schematic = Schematic.Empty
            .Add("voltage-source", 0, 0)
            .Add("resistor", 6, -2)
            .Add("ground", 0, 2)
            .Wire(0, -2, 4, -2)
            .Wire(8, -2, 8, 2)
            .Wire(0, 2, 8, 2);

        var left = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);
        var right = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal(Describe(left), Describe(right));
        Assert.Equal(left.Junctions, right.Junctions);
        Assert.Equal(
            left.Diagnostics.Select(diagnostic => diagnostic.ToString()),
            right.Diagnostics.Select(diagnostic => diagnostic.ToString()));
    }

    private static IEnumerable<string> Describe(ConnectivityResult result) =>
        result.Nets.Select(net =>
            net.Name +
            " pins=" + string.Join(",", net.Pins) +
            " wires=" + string.Join(",", net.Wires));

    private static string NetOf(ConnectivityResult result, PinRef pin)
    {
        Assert.True(result.TryGetNetOfPin(pin, out var net));

        return net.Name;
    }
}
