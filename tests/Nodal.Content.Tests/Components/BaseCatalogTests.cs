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
        Assert.Equal(ExpectedDefinitionCount, result.Catalog.Definitions.Count);
    }

    [Fact]
    public void BaseCatalogHoldsTheExpectedIdsInOrder()
    {
        var result = LoadBaseCatalog();

        Assert.Equal(ExpectedIds, result.Catalog.Definitions.Select(definition => definition.Id));
    }

    [Theory]
    [InlineData("resistor", ComponentCategory.Passive, "R", 2, 1)]
    [InlineData("capacitor", ComponentCategory.Passive, "C", 2, 1)]
    [InlineData("inductor", ComponentCategory.Passive, "L", 2, 1)]
    [InlineData("diode", ComponentCategory.Semiconductor, "D", 2, 0)]
    [InlineData("nmos", ComponentCategory.Semiconductor, "M", 3, 0)]
    [InlineData("pmos", ComponentCategory.Semiconductor, "M", 3, 0)]
    [InlineData("npn", ComponentCategory.Semiconductor, "Q", 3, 0)]
    [InlineData("pnp", ComponentCategory.Semiconductor, "Q", 3, 0)]
    [InlineData("voltage-source", ComponentCategory.Source, "V", 2, 1)]
    [InlineData("current-source", ComponentCategory.Source, "I", 2, 1)]
    [InlineData("ground", ComponentCategory.Reference, "GND", 1, 0)]
    public void BaseComponentHasTheExpectedShape(
        string id, ComponentCategory category, string designatorPrefix, int pinCount, int parameterCount)
    {
        Assert.True(LoadBaseCatalog().Catalog.TryGetById(id, out var definition));

        Assert.Equal(category, definition.Category);
        Assert.Equal(designatorPrefix, definition.DesignatorPrefix);
        Assert.Equal(pinCount, definition.Pins.Count);
        Assert.Equal(parameterCount, definition.Parameters.Count);
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
        var parameter = SingleParameterOf(id);

        Assert.Equal(parameterName, parameter.Name);
        Assert.Equal(unit, parameter.Unit);
        Assert.Equal(new Quantity(expectedValue, unit), parameter.Default);
        Assert.Equal(series, parameter.Series);
    }

    [Theory]
    [InlineData("voltage-source", "voltage", Unit.Volt, 5.0)]
    [InlineData("current-source", "current", Unit.Ampere, 1e-3)]
    public void BaseSourceParameterHasNoSeries(string id, string parameterName, Unit unit, double expectedValue)
    {
        var parameter = SingleParameterOf(id);

        Assert.Equal(parameterName, parameter.Name);
        Assert.Equal(new Quantity(expectedValue, unit), parameter.Default);
        Assert.Null(parameter.Series);
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

    private static ParameterDefinition SingleParameterOf(string id)
    {
        Assert.True(LoadBaseCatalog().Catalog.TryGetById(id, out var definition));

        return Assert.Single(definition.Parameters);
    }
}
