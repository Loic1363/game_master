using Nodal.Content.Components;
using Xunit;

namespace Nodal.Content.Tests.Components;

public class ComponentCatalogTests
{
    private static readonly PinDefinition[] SinglePin = [new PinDefinition("1", 0, 0)];

    [Fact]
    public void EmptyCatalogHoldsNoDefinition()
    {
        Assert.Empty(ComponentCatalog.Empty.Definitions);
        Assert.False(ComponentCatalog.Empty.TryGetById("resistor", out _));
    }

    [Fact]
    public void DefinitionsAreOrderedById()
    {
        var catalog = new ComponentCatalog([Definition("zener"), Definition("ammeter"), Definition("resistor")]);

        Assert.Equal(["ammeter", "resistor", "zener"], catalog.Definitions.Select(definition => definition.Id));
    }

    [Fact]
    public void LookupByIdReturnsTheMatchingDefinition()
    {
        var catalog = new ComponentCatalog([Definition("resistor")]);

        Assert.True(catalog.TryGetById("resistor", out var definition));
        Assert.Equal("resistor", definition.Id);
    }

    [Fact]
    public void DuplicateIdIsARejectedArgument()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ComponentCatalog([Definition("resistor"), Definition("resistor")]));

        Assert.Equal("definitions", exception.ParamName);
    }

    private static ComponentDefinition Definition(string id) =>
        new(id, id, id, ComponentCategory.Passive, "X", SinglePin, []);
}
