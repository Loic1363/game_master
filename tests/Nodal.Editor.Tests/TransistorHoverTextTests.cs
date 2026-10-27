using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Editor.Tests;

public class TransistorHoverTextTests
{
    [Fact]
    public void AMosfetHoverTextNamesItsRegionAndKeyQuantities()
    {
        Assert.Equal(
            "M1 (N-channel MOSFET): saturation, Vgs 3 V, Vds 5 V, Id 2.2 mA",
            Describe(BiasBox("nmos", 5, 3), "M1"));
    }

    [Fact]
    public void APmosHoverTextNamesItsRegionAndKeyQuantities()
    {
        Assert.Equal(
            "M1 (P-channel MOSFET): saturation, Vgs -3 V, Vds -5 V, Id -2.2 mA",
            Describe(BiasBox("pmos", 5, 2), "M1"));
    }

    [Fact]
    public void ACutOffMosfetHoverTextReadsCutoff()
    {
        Assert.StartsWith(
            "M1 (N-channel MOSFET): cutoff, Vgs 500 mV, Vds 5 V",
            Describe(BiasBox("nmos", 5, 0.5), "M1"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ABipolarHoverTextNamesItsRegionKeyQuantitiesAndBeta()
    {
        Assert.Equal(
            "Q1 (NPN transistor): forward active, Vbe 650 mV, Vce 5 V, "
            + "Ic 820 µA, Ib 8.2 µA, β 100",
            Describe(BiasBox("npn", 5, 0.65), "Q1"));
    }

    [Fact]
    public void APnpHoverTextMirrorsTheNpn()
    {
        Assert.Equal(
            "Q1 (PNP transistor): forward active, Vbe -650 mV, Vce -5 V, "
            + "Ic -820 µA, Ib -8.2 µA, β 100",
            Describe(BiasBox("pnp", 5, 5 - 0.65), "Q1"));
    }

    [Fact]
    public void ATwoTerminalComponentKeepsItsOldHoverText()
    {
        var schematic = Schematic.Empty
            .Add("voltage-source", 0, 0)
            .Add("ground", 0, 2)
            .Add("resistor", 6, 0, Sessions.FirstPinUp)
            .Add("ground", 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", 10)
            .Set("R1", "resistance", 1000);

        Assert.Equal("R1: 10 V, 10 mA, 100 mW absorbed", Describe(schematic, "R1"));
    }

    private static Schematic BiasBox(string definitionId, double supply, double control) =>
        Schematic.Empty
            .Add("voltage-source", 0, 0)
            .Add("ground", 0, 2)
            .Add(definitionId, 6, 6)
            .Add("ground", 6, 8)
            .Add("voltage-source", 0, 10)
            .Add("ground", 0, 12)
            .Wire(0, -2, 6, -2)
            .Wire(6, -2, 6, 4)
            .Wire(0, 8, 4, 8)
            .Wire(4, 8, 4, 6)
            .Set("V1", "voltage", supply)
            .Set("V2", "voltage", control);

    private static string? Describe(Schematic schematic, string designator)
    {
        var solution = DcAnalysis.Solve(
            NetlistCompiler.Compile(schematic, BaseContent.Catalog), BaseContent.Catalog);

        Assert.True(
            solution.IsSolved,
            string.Join("; ", solution.Diagnostics.Select(diagnostic => diagnostic.Message)));

        var id = schematic.Components.Values
            .First(component => string.Equals(
                component.Designator, designator, StringComparison.Ordinal))
            .Id;

        return OperatingPointOverlay.Describe(solution, id);
    }
}
