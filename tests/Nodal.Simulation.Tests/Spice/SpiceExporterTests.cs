using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Foundation;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Spice;
using Xunit;

namespace Nodal.Simulation.Tests.Spice;

public class SpiceExporterTests
{
    private static readonly char[] Separators = ['\n', ' ', '(', ')', ','];

    [Fact]
    public void EveryComponentKindAndParasiticMatchesTheGoldenDeck()
    {
        Assert.Equal(GoldenDeck.GoldenText(), Export(GoldenDeck.Build()));
    }

    [Fact]
    public void TheTitleLineNamesNodalAndItsVersion()
    {
        Assert.Equal(
            $"* {NodalInfo.Name} {NodalInfo.Version} export",
            Export(GoldenDeck.Build()).Split('\n')[0]);
    }

    [Fact]
    public void TheCurrentSourceValueIsFlipped()
    {
        Assert.Contains("I1 N1 0 -0.002", Lines(GoldenDeck.Build()));
    }

    [Fact]
    public void ASeriesResistanceUsesAnInternalNode()
    {
        var lines = Lines(GoldenDeck.Build());

        Assert.Contains("V1 N1 V1_int 12", lines);
        Assert.Contains("RV1 V1_int 0 10", lines);
        Assert.Contains("L1 N1 L1_int 0.001", lines);
        Assert.Contains("RL1 L1_int 0 2", lines);
    }

    [Fact]
    public void AParallelParasiticSitsAcrossTheSameTwoNets()
    {
        var lines = Lines(GoldenDeck.Build());

        Assert.Contains("RC1 N1 0 1000000", lines);
        Assert.Contains("C1_par N1 0 1E-12", lines);
        Assert.Contains("CL1_par N1 0 2E-12", lines);
        Assert.Contains("RI1 N1 0 2200", lines);
    }

    [Fact]
    public void SeveralSeriesParasiticsFormAChainOfNumberedInternalNodes()
    {
        var lines = Lines(GoldenDeck.Build());

        Assert.Contains("C1 N1 C1_int1 1E-07", lines);
        Assert.Contains("RC1_esr C1_int1 C1_int2 0.05", lines);
        Assert.Contains("LC1_esl C1_int2 0 1E-09", lines);
    }

    [Fact]
    public void ACapacitorWithoutParasiticsSitsAcrossItsOwnNets()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Capacitor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2);

        Assert.Contains("C1 N1 0 1E-07", Lines(schematic));
    }

    [Fact]
    public void TheDeckEndsWithTheModelsTheOptionsAndTheEndLine()
    {
        var lines = Lines(GoldenDeck.Build());

        Assert.Equal(".model NODAL_D D(IS=1e-14 N=1)", lines[^9]);
        Assert.StartsWith(".model NODAL_NMOS NMOS(VTO=1 ", lines[^8], StringComparison.Ordinal);
        Assert.StartsWith(".model NODAL_PMOS PMOS(VTO=-1 ", lines[^7], StringComparison.Ordinal);
        Assert.StartsWith(".model NODAL_NPN NPN(", lines[^6], StringComparison.Ordinal);
        Assert.StartsWith(".model NODAL_PNP PNP(", lines[^5], StringComparison.Ordinal);
        Assert.Equal(".model NODAL_SW_S1 SW(VT=1.5 VH=0.5 RON=2 ROFF=2000000000)", lines[^4]);
        Assert.Equal(".model NODAL_SW_SW1 SW(VT=0.5 VH=0 RON=0.02 ROFF=1000000000)", lines[^3]);
        Assert.Equal(".options TNOM=27", lines[^2]);
        Assert.Equal(".end", lines[^1]);
    }

    [Fact]
    public void ADeckDeclaresOnlyTheModelsItUses()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Nmos, 6, 6)
            .Add(Circuits.Ground, 6, 8)
            .Wire(0, -2, 6, -2)
            .Wire(6, -2, 6, 4)
            .Wire(4, 6, 4, -2)
            .Set("V1", "voltage", 5);

        var models = Lines(schematic)
            .Where(line => line.StartsWith(".model", StringComparison.Ordinal))
            .ToArray();

        Assert.StartsWith(".model NODAL_NMOS ", Assert.Single(models), StringComparison.Ordinal);
    }

    [Fact]
    public void TheDeckCarriesNoAnalysisCommand()
    {
        Assert.DoesNotContain(
            Lines(GoldenDeck.Build()),
            line => line.StartsWith(".op", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith(".options", StringComparison.Ordinal));
    }

    [Fact]
    public void RatingsAreNotExported()
    {
        var schematic = GoldenDeck.Build()
            .Set("R1", "power-rating", 0.25)
            .Set("C1", "voltage-rating", 16);

        Assert.Equal(GoldenDeck.GoldenText(), Export(schematic));
    }

    [Fact]
    public void AnIdealVoltageSourceNeedsNoInternalNode()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", 10)
            .Set("R1", "resistance", 2200);

        Assert.Equal(
            new[]
            {
                $"* {NodalInfo.Name} {NodalInfo.Version} export",
                "V1 N1 0 10",
                "R1 N1 0 2200",
                ".options TNOM=27",
                ".end",
            },
            Lines(schematic));
    }

    [Fact]
    public void AComponentTheSimulatorDoesNotModelRefusesTheExport()
    {
        Assert.False(
            SpiceExporter.TryExport(
                UnmodelledComponents.Netlist(UnmodelledComponents.Transformer),
                UnmodelledComponents.Catalog,
                out var deck,
                out var refusals));

        Assert.Empty(deck);
        Assert.Equal(
            SimulationDiagnosticCode.UnsupportedComponent, refusals.Single().Code);
    }

    [Fact]
    public void ACircuitWithoutGroundRefusesTheExport()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Wire(0, -2, 6, -2)
            .Wire(0, 2, 6, 2);

        Assert.False(
            SpiceExporter.TryExport(
                schematic.Compile(), BaseCatalog.Catalog, out var deck, out var refusals));

        Assert.Empty(deck);
        Assert.Equal(SimulationDiagnosticCode.NoGround, refusals.Single().Code);
        Assert.Equal(
            "The circuit has no net named 0, so the DC analysis has no voltage reference.",
            refusals.Single().Message);
    }

    [Fact]
    public void ARefusedExportThrowsFromTheSimpleEntryPoint()
    {
        var netlist = UnmodelledComponents.Netlist(UnmodelledComponents.Transformer);

        var exception = Assert.Throws<InvalidOperationException>(
            () => SpiceExporter.Export(netlist, UnmodelledComponents.Catalog));

        Assert.Contains("X1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANegatedZeroIsPrintedWithoutItsSign()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.CurrentSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("R1", "resistance", 1000)
            .Function("I1", new PulseWaveform(0, 1e-3, 0, 1e-9, 1e-9, 1e-6, 2e-6));

        Assert.Contains("I1 N1 0 PULSE(0 -0.001 0 1E-09 1E-09 1E-06 2E-06)", Lines(schematic));
        Assert.DoesNotContain("-0", Tokens(schematic));
    }

    [Fact]
    public void ExportingTheSameNetlistTwiceGivesTheSameText()
    {
        var netlist = GoldenDeck.Build().Compile();

        Assert.Equal(
            SpiceExporter.Export(netlist, BaseCatalog.Catalog),
            SpiceExporter.Export(netlist, BaseCatalog.Catalog));
    }

    private static string[] Tokens(Schematic schematic) =>
        Export(schematic).Split(Separators);

    private static string Export(Schematic schematic) =>
        SpiceExporter.Export(schematic.Compile(), BaseCatalog.Catalog);

    private static string[] Lines(Schematic schematic) =>
        Export(schematic).TrimEnd('\n').Split('\n');
}
