using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Tests.Connectivity;
using Nodal.Foundation.Quantities;
using Xunit;
using static Nodal.Circuit.Tests.Connectivity.ConnectivityTests;

namespace Nodal.Circuit.Tests.Netlists;

public class NetlistTests
{
    [Fact]
    public void AnEmptySchematicCompilesToAnEmptyNetlist()
    {
        var netlist = NetlistCompiler.Compile(Schematic.Empty, BaseCatalog.Catalog);

        Assert.Empty(netlist.Elements);
        Assert.Empty(netlist.NetNames);
        Assert.Empty(netlist.Diagnostics);
        Assert.Equal(string.Empty, netlist.ToDebugText());
    }

    [Fact]
    public void ElementsFollowAscendingComponentOrder()
    {
        var schematic = Schematic.Empty
            .Add("capacitor", 0, 0)
            .Add("resistor", 20, 0)
            .Add("ground", 40, 0);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        Assert.Equal([1, 2, 3], netlist.Elements.Select(element => element.Id.Value));
        Assert.Equal(["C1", "R1", "GND1"], netlist.Elements.Select(element => element.Designator));
        Assert.Equal(
            ["capacitor", "resistor", "ground"],
            netlist.Elements.Select(element => element.DefinitionId));
    }

    [Fact]
    public void PinsMapToTheirNetsInDefinitionOrder()
    {
        var schematic = Schematic.Empty.Add("npn", 0, 0).Add("ground", 0, 2);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);
        var transistor = netlist.Elements[0];

        Assert.Equal(
            ["collector", "base", "emitter"],
            transistor.Pins.Select(pin => pin.PinName));
        Assert.Equal("0", transistor.Pins.Single(pin => pin.PinName == "emitter").NetName);
    }

    [Fact]
    public void TwoResistorsInSeriesShareTheMiddleNet()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 10, 0)
            .Wire(2, 0, 8, 0);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        Assert.Equal(["N1", "N2"], netlist.Elements[0].Pins.Select(pin => pin.NetName));
        Assert.Equal(["N2", "N3"], netlist.Elements[1].Pins.Select(pin => pin.NetName));
    }

    [Fact]
    public void ParametersAreCarriedThrough()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        var parameter = Assert.Single(netlist.Elements[0].Parameters);
        Assert.Equal("resistance", parameter.Key);
        Assert.Equal(new Quantity(1000, Unit.Ohm), parameter.Value);
    }

    [Fact]
    public void AComponentWithoutParametersCarriesNone()
    {
        var schematic = Schematic.Empty.Add("ground", 0, 0);

        Assert.Empty(NetlistCompiler.Compile(schematic, BaseCatalog.Catalog).Elements[0].Parameters);
    }

    [Fact]
    public void NetNamesComeFromConnectivity()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0).Add("ground", 2, 0);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        Assert.Equal(["0", "N1"], netlist.NetNames);
    }

    [Fact]
    public void DiagnosticsArePassedThrough()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        Assert.Equal(
            [
                ConnectivityDiagnosticCode.FloatingPin,
                ConnectivityDiagnosticCode.FloatingPin,
                ConnectivityDiagnosticCode.NoGround,
            ],
            netlist.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public void AComponentWithAnUnknownDefinitionIsLeftOut()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("widget", 40, 40, Circuits.WidgetCatalog())
            .Add("ground", 2, 0);

        var netlist = NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

        Assert.Equal(["R1", "GND1"], netlist.Elements.Select(element => element.Designator));
        Assert.Contains(
            netlist.Diagnostics,
            diagnostic => diagnostic.Code == ConnectivityDiagnosticCode.UnknownDefinition);
    }

    [Fact]
    public void AnRcCircuitCompilesToTheExpectedDebugText()
    {
        var netlist = NetlistCompiler.Compile(RcCircuit(), BaseCatalog.Catalog);

        Assert.Equal(
            $"""
            V1 voltage-source plus=N1 minus=0 voltage=5 V
            R1 resistor 1=N1 2=N2 resistance=1 k{UnitSymbols.Ohm}
            C1 capacitor 1=N2 2=0 capacitance=100 nF
            GND1 ground gnd=0
            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            netlist.ToDebugText());
    }

    [Fact]
    public void TheRcCircuitIsWellFormed()
    {
        var netlist = NetlistCompiler.Compile(RcCircuit(), BaseCatalog.Catalog);

        Assert.Empty(netlist.Diagnostics);
        Assert.Equal(["0", "N1", "N2"], netlist.NetNames);
    }

    [Fact]
    public void TheDebugTextEndsWithoutANewline()
    {
        var text = NetlistCompiler.Compile(RcCircuit(), BaseCatalog.Catalog).ToDebugText();

        Assert.False(text.EndsWith('\n'));
        Assert.Equal(4, text.Split('\n').Length);
    }

    [Fact]
    public void CompilingNeedsASchematicAndACatalog()
    {
        Assert.Equal(
            "schematic",
            Assert.Throws<ArgumentNullException>(
                () => NetlistCompiler.Compile(null!, BaseCatalog.Catalog)).ParamName);
        Assert.Equal(
            "catalog",
            Assert.Throws<ArgumentNullException>(
                () => NetlistCompiler.Compile(Schematic.Empty, null!)).ParamName);
    }

    private static Schematic RcCircuit() =>
        Schematic.Empty
            .Add("voltage-source", 0, 0)
            .Add("resistor", 6, -2)
            .Add("capacitor", 12, -2)
            .Add("ground", 0, 2)
            .Wire(0, -2, 4, -2)
            .Wire(8, -2, 10, -2)
            .Wire(14, -2, 14, 2)
            .Wire(14, 2, 0, 2);
}
