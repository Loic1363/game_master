using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Foundation;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Spice;
using Xunit;

namespace Nodal.Simulation.Tests.Spice;

public class SpiceExporterTests
{
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

        Assert.Equal(".model NODAL_D D(IS=1e-14 N=1)", lines[^7]);
        Assert.StartsWith(".model NODAL_NMOS NMOS(VTO=1 ", lines[^6], StringComparison.Ordinal);
        Assert.StartsWith(".model NODAL_PMOS PMOS(VTO=-1 ", lines[^5], StringComparison.Ordinal);
        Assert.StartsWith(".model NODAL_NPN NPN(", lines[^4], StringComparison.Ordinal);
        Assert.StartsWith(".model NODAL_PNP PNP(", lines[^3], StringComparison.Ordinal);
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
    public void ExportingTheSameNetlistTwiceGivesTheSameText()
    {
        var netlist = GoldenDeck.Build().Compile();

        Assert.Equal(
            SpiceExporter.Export(netlist, BaseCatalog.Catalog),
            SpiceExporter.Export(netlist, BaseCatalog.Catalog));
    }

    private static string Export(Schematic schematic) =>
        SpiceExporter.Export(schematic.Compile(), BaseCatalog.Catalog);

    private static string[] Lines(Schematic schematic) =>
        Export(schematic).TrimEnd('\n').Split('\n');
}
