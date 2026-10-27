using Nodal.Circuit.Schematics;
using Nodal.Simulation.Spice;
using Xunit;

namespace Nodal.Simulation.Tests.Spice;

public class TransistorExportTests
{
    [Theory]
    [InlineData(Circuits.Nmos, "M1 N1 N1 0 0 NODAL_NMOS W=1e-6 L=1e-6")]
    [InlineData(Circuits.Pmos, "M1 0 N1 N1 N1 NODAL_PMOS W=1e-6 L=1e-6")]
    [InlineData(Circuits.Npn, "Q1 N1 N1 0 NODAL_NPN")]
    [InlineData(Circuits.Pnp, "Q1 0 N1 N1 NODAL_PNP")]
    public void EveryTransistorExportsItsOwnElementLine(string definitionId, string expected)
    {
        Assert.Contains(expected, Lines(definitionId));
    }

    [Theory]
    [InlineData(Circuits.Nmos, "NODAL_NMOS")]
    [InlineData(Circuits.Pmos, "NODAL_PMOS")]
    [InlineData(Circuits.Npn, "NODAL_NPN")]
    [InlineData(Circuits.Pnp, "NODAL_PNP")]
    public void OnlyTheModelOfThePlacedTransistorIsDeclared(
        string definitionId, string modelName)
    {
        var models = Lines(definitionId)
            .Where(line => line.StartsWith(".model", StringComparison.Ordinal))
            .ToArray();

        Assert.StartsWith(
            ".model " + modelName + " ", Assert.Single(models), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Circuits.Nmos)]
    [InlineData(Circuits.Pmos)]
    public void AMosfetTiesItsBodyToItsSourceAndHasEqualWidthAndLength(string definitionId)
    {
        var line = Lines(definitionId).Single(
            candidate => candidate.StartsWith("M1 ", StringComparison.Ordinal));

        var fields = line.Split(' ');

        Assert.Equal(fields[3], fields[4]);
        Assert.Equal("W=1e-6", fields[6]);
        Assert.Equal("L=1e-6", fields[7]);
    }

    [Theory]
    [InlineData(Circuits.Npn)]
    [InlineData(Circuits.Pnp)]
    public void ABipolarExportsThreeNodesAndNoGeometry(string definitionId)
    {
        var line = Lines(definitionId).Single(
            candidate => candidate.StartsWith("Q1 ", StringComparison.Ordinal));

        Assert.Equal(5, line.Split(' ').Length);
    }

    [Theory]
    [InlineData(Circuits.Nmos, "VTO=1")]
    [InlineData(Circuits.Pmos, "VTO=-1")]
    public void TheMosfetModelCarriesTheParametersOfTheDeviceModel(
        string definitionId, string threshold)
    {
        var model = ModelLine(definitionId);

        Assert.Contains(threshold, model, StringComparison.Ordinal);
        Assert.Contains("LEVEL=1", model, StringComparison.Ordinal);
        Assert.Contains("KP=1e-3", model, StringComparison.Ordinal);
        Assert.Contains("LAMBDA=0.02", model, StringComparison.Ordinal);
        Assert.Contains("GAMMA=0", model, StringComparison.Ordinal);
        Assert.Contains("IS=1e-14", model, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Circuits.Nmos)]
    [InlineData(Circuits.Pmos)]
    public void TheMosfetModelPinsEveryCapacitanceToZero(string definitionId)
    {
        var model = ModelLine(definitionId);

        foreach (var parameter in new[]
            { "TOX=0", "CGSO=0", "CGDO=0", "CGBO=0", "CBD=0", "CBS=0", "CJ=0", "CJSW=0" })
        {
            Assert.Contains(parameter, model, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(Circuits.Npn)]
    [InlineData(Circuits.Pnp)]
    public void TheBipolarModelDisablesEveryGummelPoonExtra(string definitionId)
    {
        var model = ModelLine(definitionId);

        foreach (var parameter in new[]
            {
                "IS=1e-14", "BF=100", "BR=1", "NF=1", "NR=1",
                "VAF=0", "VAR=0", "IKF=0", "IKR=0", "ISE=0", "ISC=0",
                "RB=0", "RE=0", "RC=0", "CJE=0", "CJC=0", "CJS=0", "TF=0", "TR=0",
            })
        {
            Assert.Contains(parameter, model, StringComparison.Ordinal);
        }
    }

    private static string ModelLine(string definitionId) => Lines(definitionId).Single(
        line => line.StartsWith(".model", StringComparison.Ordinal));

    private static string[] Lines(string definitionId) => SpiceExporter
        .Export(Circuit(definitionId).Compile(), BaseCatalog.Catalog)
        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static Schematic Circuit(string definitionId) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(definitionId, 6, 6)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, -2, 6, 4)
        .Wire(4, 6, 4, -2)
        .Set("V1", "voltage", 5);
}
