using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Connectivity;

public class ConnectivityTests
{
    [Fact]
    public void AnEmptySchematicHasNoNets()
    {
        var result = ConnectivityAnalyzer.Analyze(Schematic.Empty, BaseCatalog.Catalog);

        Assert.Empty(result.Nets);
        Assert.Empty(result.Junctions);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TwoResistorsSharingAWireAreInSeries()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 10, 0)
            .Wire(2, 0, 8, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal(["N1", "N2", "N3"], result.NetNames);
        AssertSameNet(result, Pin(1, "2"), Pin(2, "1"));
        AssertDifferentNets(result, Pin(1, "1"), Pin(1, "2"));
        AssertDifferentNets(result, Pin(2, "1"), Pin(2, "2"));
    }

    [Fact]
    public void TheSharedWireBelongsToTheSharedNet()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 10, 0)
            .Wire(2, 0, 8, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.True(result.TryGetNetOfWire(WireId.First, out var net));
        Assert.Equal("N2", net.Name);
        Assert.Equal([Pin(1, "2"), Pin(2, "1")], net.Pins);
        Assert.Equal([WireId.First], net.Wires);
    }

    [Fact]
    public void AWireEndpointOnAnotherWireInteriorConnects()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 10, 0)
            .Wire(5, 0, 5, 5);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        var net = Assert.Single(result.Nets);
        Assert.Equal([new WireId(1), new WireId(2)], net.Wires);
    }

    [Fact]
    public void CrossingWireInteriorsDoNotConnect()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 10, 0)
            .Wire(5, -5, 5, 5);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Equal(2, result.Nets.Length);
        Assert.Equal(["N1", "N2"], result.NetNames);
    }

    [Fact]
    public void APinOnAWireInteriorConnects()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 3, 0)
            .Wire(5, -5, 5, 5);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.True(result.TryGetNetOfPin(Pin(1, "2"), out var net));
        Assert.Equal([WireId.First], net.Wires);
        AssertDifferentNets(result, Pin(1, "1"), Pin(1, "2"));
    }

    [Fact]
    public void TwoPinsTouchingDirectlyConnectWithoutAWire()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 4, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        AssertSameNet(result, Pin(1, "2"), Pin(2, "1"));
        Assert.Equal(3, result.Nets.Length);
    }

    [Fact]
    public void AWireTouchingNothingFormsItsOwnNet()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 6, 0);

        var net = Assert.Single(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog).Nets);

        Assert.Equal("N1", net.Name);
        Assert.Empty(net.Pins);
        Assert.Equal([WireId.First], net.Wires);
    }

    [Fact]
    public void SeveralGroundsMergeIntoTheGroundNet()
    {
        var schematic = Schematic.Empty
            .Add("ground", 0, 0)
            .Add("ground", 20, 0)
            .Add("ground", 40, 40);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        var net = Assert.Single(result.Nets);
        Assert.Equal("0", net.Name);
        Assert.Equal([Pin(1, "gnd"), Pin(2, "gnd"), Pin(3, "gnd")], net.Pins);
    }

    [Fact]
    public void AGroundedResistorReachesTheGroundNetThroughAWire()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("ground", 10, 0)
            .Wire(2, 0, 10, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.True(result.TryGetNetOfPin(Pin(1, "2"), out var net));
        Assert.Equal("0", net.Name);
        Assert.Equal([Pin(1, "2"), Pin(2, "gnd")], net.Pins);
    }

    [Fact]
    public void ARotatedComponentConnectsThroughItsTransformedPin()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 2, 2, new Orientation(Rotation.R90, false));

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        AssertSameNet(result, Pin(1, "2"), Pin(2, "1"));
    }

    [Fact]
    public void AMirroredComponentConnectsThroughItsTransformedPin()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 4, 0, new Orientation(Rotation.R0, true));

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        AssertSameNet(result, Pin(1, "2"), Pin(2, "2"));
    }

    [Fact]
    public void ARotatedAndAMirroredComponentMeetAtOnePoint()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 2, 2, new Orientation(Rotation.R90, false))
            .Add("resistor", 4, 0, new Orientation(Rotation.R0, true));

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.True(result.TryGetNetOfPin(Pin(1, "2"), out var net));
        Assert.Equal([Pin(1, "2"), Pin(2, "1"), Pin(3, "2")], net.Pins);
    }

    [Fact]
    public void ATransistorConnectsOnAllThreePins()
    {
        var schematic = Schematic.Empty
            .Add("npn", 0, 0)
            .Add("ground", 0, 2);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.True(result.TryGetNetOfPin(Pin(1, "emitter"), out var emitter));
        Assert.Equal("0", emitter.Name);
        Assert.True(result.TryGetNetOfPin(Pin(1, "collector"), out _));
        Assert.True(result.TryGetNetOfPin(Pin(1, "base"), out _));
        Assert.Equal(3, result.Nets.Length);
    }

    [Fact]
    public void EveryPinOfAKnownComponentBelongsToSomeNet()
    {
        var schematic = Schematic.Empty.Add("npn", 0, 0).Add("resistor", 20, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        foreach (var pin in new[] { "collector", "base", "emitter" })
        {
            Assert.True(result.TryGetNetOfPin(Pin(1, pin), out _));
        }

        Assert.True(result.TryGetNetOfPin(Pin(2, "1"), out _));
        Assert.True(result.TryGetNetOfPin(Pin(2, "2"), out _));
    }

    [Fact]
    public void AnUnknownPinHasNoNet()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.False(result.TryGetNetOfPin(Pin(1, "3"), out var net));
        Assert.Null(net);
        Assert.False(result.TryGetNetOfWire(new WireId(9), out var wire));
        Assert.Null(wire);
    }

    [Fact]
    public void AnalyzingNeedsASchematicAndACatalog()
    {
        Assert.Equal(
            "schematic",
            Assert.Throws<ArgumentNullException>(
                () => ConnectivityAnalyzer.Analyze(null!, BaseCatalog.Catalog)).ParamName);
        Assert.Equal(
            "catalog",
            Assert.Throws<ArgumentNullException>(
                () => ConnectivityAnalyzer.Analyze(Schematic.Empty, null!)).ParamName);
    }

    internal static PinRef Pin(int component, string pinName) =>
        new(new ComponentId(component), pinName);

    private static void AssertSameNet(ConnectivityResult result, PinRef left, PinRef right)
    {
        Assert.True(result.TryGetNetOfPin(left, out var leftNet));
        Assert.True(result.TryGetNetOfPin(right, out var rightNet));
        Assert.Equal(leftNet.Name, rightNet.Name);
    }

    private static void AssertDifferentNets(ConnectivityResult result, PinRef left, PinRef right)
    {
        Assert.True(result.TryGetNetOfPin(left, out var leftNet));
        Assert.True(result.TryGetNetOfPin(right, out var rightNet));
        Assert.NotEqual(leftNet.Name, rightNet.Name);
    }
}
