using Nodal.Content.Components;
using Nodal.Content.Sources;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Content.Tests.Components;

public class BaseCatalogTests
{
    private const int ExpectedDefinitionCount = 11;

    private static readonly string[] ExpectedIds =
    [
        "capacitor", "current-source", "diode", "ground", "inductor", "nmos",
        "npn", "pmos", "pnp", "resistor", "voltage-source",
    ];

    private static ComponentCatalogLoadResult LoadBaseCatalog() =>
        ComponentCatalogLoader.Load(new DirectoryContentSource(RepositoryPaths.ComponentsDirectory));

    [Fact]
    public void BaseCatalogLoadsWithoutDiagnostics()
    {
        var result = LoadBaseCatalog();

        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExpectedDefinitionCount, result.Catalog.Definitions.Length);
    }

    [Fact]
    public void BaseCatalogHoldsTheExpectedIdsInOrder()
    {
        var result = LoadBaseCatalog();

        Assert.Equal(ExpectedIds, result.Catalog.Definitions.Select(definition => definition.Id));
    }

    [Theory]
    [InlineData("resistor", ComponentCategory.Passive, "R", 2, 2)]
    [InlineData("capacitor", ComponentCategory.Passive, "C", 2, 7)]
    [InlineData("inductor", ComponentCategory.Passive, "L", 2, 4)]
    [InlineData("diode", ComponentCategory.Semiconductor, "D", 2, 0)]
    [InlineData("nmos", ComponentCategory.Semiconductor, "M", 3, 0)]
    [InlineData("pmos", ComponentCategory.Semiconductor, "M", 3, 0)]
    [InlineData("npn", ComponentCategory.Semiconductor, "Q", 3, 0)]
    [InlineData("pnp", ComponentCategory.Semiconductor, "Q", 3, 0)]
    [InlineData("voltage-source", ComponentCategory.Source, "V", 2, 2)]
    [InlineData("current-source", ComponentCategory.Source, "I", 2, 2)]
    [InlineData("ground", ComponentCategory.Reference, "GND", 1, 0)]
    public void BaseComponentHasTheExpectedShape(
        string id, ComponentCategory category, string designatorPrefix, int pinCount, int parameterCount)
    {
        Assert.True(LoadBaseCatalog().Catalog.TryGetById(id, out var definition));

        Assert.Equal(category, definition.Category);
        Assert.Equal(designatorPrefix, definition.DesignatorPrefix);
        Assert.Equal(pinCount, definition.Pins.Length);
        Assert.Equal(parameterCount, definition.Parameters.Length);
        Assert.NotEmpty(definition.Name);
        Assert.NotEmpty(definition.Description);
    }

    [Theory]
    [InlineData("resistor", "resistance", Unit.Ohm, 1e3, StandardSeries.E24)]
    [InlineData("capacitor", "capacitance", Unit.Farad, 100e-9, StandardSeries.E12)]
    [InlineData("inductor", "inductance", Unit.Henry, 10e-6, StandardSeries.E12)]
    public void BasePassiveParameterCarriesItsDefaultAndSeries(
        string id, string parameterName, Unit unit, double expectedValue, StandardSeries series)
    {
        var parameter = ParameterOf(id, parameterName);

        Assert.Equal(unit, parameter.Unit);
        Assert.Equal(new Quantity(expectedValue, unit), parameter.Default);
        Assert.Equal(series, parameter.Series);
        Assert.True(parameter.IsRequired);
    }

    [Theory]
    [InlineData("voltage-source", "voltage", Unit.Volt, 5.0)]
    [InlineData("current-source", "current", Unit.Ampere, 1e-3)]
    public void BaseSourceParameterHasNoSeries(string id, string parameterName, Unit unit, double expectedValue)
    {
        var parameter = ParameterOf(id, parameterName);

        Assert.Equal(new Quantity(expectedValue, unit), parameter.Default);
        Assert.Null(parameter.Series);
    }

    [Theory]
    [InlineData("resistor", "resistance", "Resistance")]
    [InlineData("capacitor", "capacitance", "Capacitance")]
    [InlineData("inductor", "inductance", "Inductance")]
    [InlineData("voltage-source", "voltage", "Voltage")]
    [InlineData("current-source", "current", "Current")]
    public void BaseBasicParameterCarriesItsLabelAndTier(string id, string parameterName, string label)
    {
        var parameter = ParameterOf(id, parameterName);

        Assert.Equal(label, parameter.Label);
        Assert.Equal(ParameterTier.Basic, parameter.Tier);
    }

    [Theory]
    [InlineData("resistor", "power-rating", "Power rating", Unit.Watt)]
    [InlineData("capacitor", "voltage-rating", "Voltage rating", Unit.Volt)]
    [InlineData("capacitor", "rms-current-rating", "RMS current rating", Unit.Ampere)]
    [InlineData("capacitor", "esr", "Equivalent series resistance", Unit.Ohm)]
    [InlineData("capacitor", "esl", "Equivalent series inductance", Unit.Henry)]
    [InlineData("capacitor", "parallel-resistance", "Equivalent parallel resistance", Unit.Ohm)]
    [InlineData("capacitor", "parallel-capacitance", "Equivalent parallel capacitance", Unit.Farad)]
    [InlineData("inductor", "series-resistance", "Series resistance", Unit.Ohm)]
    [InlineData("inductor", "current-rating", "Current rating", Unit.Ampere)]
    [InlineData("inductor", "parallel-capacitance", "Parallel capacitance", Unit.Farad)]
    [InlineData("voltage-source", "series-resistance", "Series resistance", Unit.Ohm)]
    [InlineData("current-source", "parallel-resistance", "Parallel resistance", Unit.Ohm)]
    public void BaseAdvancedParameterIsOptionalAndNotNegative(
        string id, string parameterName, string label, Unit unit)
    {
        var parameter = ParameterOf(id, parameterName);

        Assert.Equal(label, parameter.Label);
        Assert.Equal(ParameterTier.Advanced, parameter.Tier);
        Assert.Equal(unit, parameter.Unit);
        Assert.Null(parameter.Default);
        Assert.False(parameter.IsRequired);
        Assert.Equal(new Quantity(0, unit), parameter.Minimum);
        Assert.Null(parameter.Maximum);
    }

    [Fact]
    public void EveryBaseParameterCarriesALabelAndExactlyOneBasicRequiredParameter()
    {
        foreach (var definition in LoadBaseCatalog().Catalog.Definitions)
        {
            Assert.All(definition.Parameters, parameter => Assert.NotEmpty(parameter.Label));

            Assert.Equal(
                definition.Parameters.Count(parameter => parameter.IsRequired),
                definition.Parameters.Count(parameter => parameter.Tier == ParameterTier.Basic));
        }
    }

    [Theory]
    [InlineData("resistor", "1", -2, 0)]
    [InlineData("resistor", "2", 2, 0)]
    [InlineData("diode", "anode", -2, 0)]
    [InlineData("diode", "cathode", 2, 0)]
    [InlineData("nmos", "drain", 0, -2)]
    [InlineData("nmos", "source", 0, 2)]
    [InlineData("pmos", "drain", 0, 2)]
    [InlineData("pmos", "source", 0, -2)]
    [InlineData("npn", "collector", 0, -2)]
    [InlineData("pnp", "collector", 0, 2)]
    [InlineData("ground", "gnd", 0, 0)]
    public void BasePinSitsAtTheExpectedGridPosition(string id, string pinName, int x, int y)
    {
        Assert.True(LoadBaseCatalog().Catalog.TryGetById(id, out var definition));

        var pin = Assert.Single(definition.Pins, candidate => candidate.Name == pinName);

        Assert.Equal(pinName, pin.Name);
        Assert.Equal(x, pin.X);
        Assert.Equal(y, pin.Y);
    }

    [Fact]
    public void UnknownIdIsNotFound()
    {
        Assert.False(LoadBaseCatalog().Catalog.TryGetById("transformer", out var definition));
        Assert.Null(definition);
    }

    private static ParameterDefinition ParameterOf(string id, string parameterName)
    {
        Assert.True(LoadBaseCatalog().Catalog.TryGetById(id, out var definition));

        return Assert.Single(
            definition.Parameters, candidate => candidate.Name == parameterName);
    }
}
