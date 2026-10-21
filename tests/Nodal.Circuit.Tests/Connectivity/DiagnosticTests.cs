using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Xunit;
using static Nodal.Circuit.Tests.Connectivity.ConnectivityTests;

namespace Nodal.Circuit.Tests.Connectivity;

public class DiagnosticTests
{
    [Fact]
    public void ALoneResistorReportsBothPinsAsFloating()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var floating = Analyze(schematic, ConnectivityDiagnosticCode.FloatingPin);

        Assert.Equal(2, floating.Length);
        Assert.Equal(ConnectivitySeverity.Warning, floating[0].Severity);
        Assert.Equal([Pin(1, "1")], floating[0].Pins);
        Assert.Equal([Pin(1, "2")], floating[1].Pins);
        Assert.Equal([new GridPoint(-2, 0)], floating[0].Positions);
        Assert.Equal([new ComponentId(1)], floating[0].Components);
    }

    [Fact]
    public void FloatingPinsAreReportedInDefinitionPinOrder()
    {
        var schematic = Schematic.Empty.Add("npn", 0, 0);

        var floating = Analyze(schematic, ConnectivityDiagnosticCode.FloatingPin);

        Assert.Equal(
            [Pin(1, "collector"), Pin(1, "base"), Pin(1, "emitter")],
            floating.Select(diagnostic => diagnostic.Pins[0]));
    }

    [Fact]
    public void AConnectedPinIsNotFloating()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 4, 0);

        var floating = Analyze(schematic, ConnectivityDiagnosticCode.FloatingPin);

        Assert.Equal([Pin(1, "1"), Pin(2, "2")], floating.Select(diagnostic => diagnostic.Pins[0]));
    }

    [Fact]
    public void AWireOnlyNetReportsNoFloatingPin()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 6, 0);

        Assert.Empty(Analyze(schematic, ConnectivityDiagnosticCode.FloatingPin));
    }

    [Fact]
    public void AWireEndTouchingNothingIsDangling()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Wire(2, 0, 8, 0);

        var dangling = Assert.Single(Analyze(schematic, ConnectivityDiagnosticCode.DanglingWireEnd));

        Assert.Equal(ConnectivitySeverity.Warning, dangling.Severity);
        Assert.Equal([WireId.First], dangling.Wires);
        Assert.Equal([new GridPoint(8, 0)], dangling.Positions);
    }

    [Fact]
    public void ALoneWireReportsBothEndsAsDangling()
    {
        var schematic = Schematic.Empty.Wire(0, 0, 6, 0);

        var dangling = Analyze(schematic, ConnectivityDiagnosticCode.DanglingWireEnd);

        Assert.Equal(2, dangling.Length);
        Assert.Equal(
            [new GridPoint(0, 0), new GridPoint(6, 0)],
            dangling.Select(diagnostic => diagnostic.Positions[0]));
    }

    [Fact]
    public void AWireEndCarryingAPinIsNotDangling()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("resistor", 14, 0)
            .Wire(2, 0, 12, 0);

        Assert.Empty(Analyze(schematic, ConnectivityDiagnosticCode.DanglingWireEnd));
    }

    [Fact]
    public void AWireEndMeetingAnotherWireIsNotDangling()
    {
        var schematic = Schematic.Empty
            .Wire(0, 0, 6, 0)
            .Wire(6, 0, 6, 6);

        var dangling = Analyze(schematic, ConnectivityDiagnosticCode.DanglingWireEnd);

        Assert.Equal(2, dangling.Length);
        Assert.DoesNotContain(new GridPoint(6, 0), dangling.Select(entry => entry.Positions[0]));
    }

    [Fact]
    public void ASchematicWithoutAGroundIsReported()
    {
        var schematic = Schematic.Empty.Add("resistor", 0, 0);

        var diagnostic = Assert.Single(Analyze(schematic, ConnectivityDiagnosticCode.NoGround));

        Assert.Equal(ConnectivitySeverity.Warning, diagnostic.Severity);
        Assert.Empty(diagnostic.Components);
    }

    [Fact]
    public void ASchematicWithAGroundIsNotReported()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("ground", 2, 0);

        Assert.Empty(Analyze(schematic, ConnectivityDiagnosticCode.NoGround));
    }

    [Fact]
    public void AnEmptySchematicNeedsNoGround()
    {
        Assert.Empty(Analyze(Schematic.Empty, ConnectivityDiagnosticCode.NoGround));
    }

    [Fact]
    public void AWireOnlySchematicNeedsNoGround()
    {
        Assert.Empty(Analyze(Schematic.Empty.Wire(0, 0, 6, 0), ConnectivityDiagnosticCode.NoGround));
    }

    [Fact]
    public void TwoGlobalNetsOnOneNetAreAShort()
    {
        var catalog = Circuits.GlobalNetCatalog();
        var schematic = Schematic.Empty
            .Add("gnd-symbol", 0, 0, catalog)
            .Add("vcc-symbol", 0, 0, catalog);

        var result = ConnectivityAnalyzer.Analyze(schematic, catalog);
        var diagnostic = Assert.Single(
            Of(result, ConnectivityDiagnosticCode.GlobalNetShort));

        Assert.Equal(ConnectivitySeverity.Error, diagnostic.Severity);
        Assert.True(result.HasErrors);
        Assert.Contains("0", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("VCC", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal([Pin(1, "p"), Pin(2, "p")], diagnostic.Pins);
    }

    [Fact]
    public void AShortedNetTakesTheAlphabeticallyFirstGlobalName()
    {
        var catalog = Circuits.GlobalNetCatalog();
        var schematic = Schematic.Empty
            .Add("vcc-symbol", 0, 0, catalog)
            .Add("gnd-symbol", 10, 0, catalog)
            .Wire(0, 0, 10, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, catalog);

        var net = Assert.Single(result.Nets);
        Assert.Equal("0", net.Name);
        Assert.Equal(["0"], result.NetNames);
    }

    [Fact]
    public void SeparateGlobalNetsAreNotAShort()
    {
        var catalog = Circuits.GlobalNetCatalog();
        var schematic = Schematic.Empty
            .Add("gnd-symbol", 0, 0, catalog)
            .Add("vcc-symbol", 20, 0, catalog);

        var result = ConnectivityAnalyzer.Analyze(schematic, catalog);

        Assert.Empty(Of(result, ConnectivityDiagnosticCode.GlobalNetShort));
        Assert.Equal(["0", "VCC"], result.NetNames);
    }

    [Fact]
    public void AComponentWithAnUnknownDefinitionIsReportedAndItsPinsSkipped()
    {
        var schematic = Schematic.Empty.Add("widget", 0, 0, Circuits.WidgetCatalog());

        var result = ConnectivityAnalyzer.Analyze(schematic, ComponentCatalog.Empty);
        var diagnostic = Assert.Single(
            Of(result, ConnectivityDiagnosticCode.UnknownDefinition));

        Assert.Equal(ConnectivitySeverity.Error, diagnostic.Severity);
        Assert.Equal([new ComponentId(1)], diagnostic.Components);
        Assert.Contains("widget", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(result.Nets);
        Assert.False(result.TryGetNetOfPin(Pin(1, "a"), out _));
    }

    [Fact]
    public void KnownComponentsStillConnectAroundAnUnknownOne()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("widget", 40, 40, Circuits.WidgetCatalog())
            .Add("resistor", 4, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Single(Of(result, ConnectivityDiagnosticCode.UnknownDefinition));
        Assert.True(result.TryGetNetOfPin(Pin(1, "2"), out var net));
        Assert.Equal([Pin(1, "2"), Pin(3, "1")], net.Pins);
    }

    [Fact]
    public void DiagnosticsComeInAFixedOrder()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("widget", 40, 40, Circuits.WidgetCatalog())
            .Wire(20, 0, 26, 0);

        var codes = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog)
            .Diagnostics
            .Select(diagnostic => diagnostic.Code);

        Assert.Equal(
            [
                ConnectivityDiagnosticCode.UnknownDefinition,
                ConnectivityDiagnosticCode.FloatingPin,
                ConnectivityDiagnosticCode.FloatingPin,
                ConnectivityDiagnosticCode.DanglingWireEnd,
                ConnectivityDiagnosticCode.DanglingWireEnd,
                ConnectivityDiagnosticCode.NoGround,
            ],
            codes);
    }

    [Fact]
    public void AWellFormedCircuitReportsNothing()
    {
        var schematic = Schematic.Empty
            .Add("resistor", 0, 0)
            .Add("ground", 2, 0)
            .Add("ground", 20, 0)
            .Wire(-2, 0, 20, 0);

        var result = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);

        Assert.Empty(result.Diagnostics);
        Assert.False(result.HasErrors);
    }

    private static ConnectivityDiagnostic[] Analyze(
        Schematic schematic, ConnectivityDiagnosticCode code) =>
        Of(ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog), code);

    private static ConnectivityDiagnostic[] Of(
        ConnectivityResult result, ConnectivityDiagnosticCode code) =>
        [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == code)];
}
