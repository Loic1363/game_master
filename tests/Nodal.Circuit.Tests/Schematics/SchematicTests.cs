using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Schematics;

public class SchematicTests
{
    [Fact]
    public void TheEmptySchematicHoldsNothingAndStartsItsIdentifiersAtOne()
    {
        Assert.Empty(Schematic.Empty.Components);
        Assert.Empty(Schematic.Empty.Wires);
        Assert.Equal(ComponentId.First, Schematic.Empty.NextComponentId);
        Assert.Equal(WireId.First, Schematic.Empty.NextWireId);
    }

    [Fact]
    public void ComponentsEnumerateInAscendingIdentifierOrder()
    {
        var schematic = Schematic.Empty;

        for (var index = 0; index < 5; index++)
        {
            schematic = new AddComponent("resistor", new GridPoint(index, 0), Orientation.Default)
                .Succeed(schematic);
        }

        schematic = new RemoveComponent(new ComponentId(2)).Succeed(schematic);
        schematic = new AddComponent("capacitor", new GridPoint(9, 9), Orientation.Default)
            .Succeed(schematic);

        Assert.Equal([1, 3, 4, 5, 6], schematic.Components.Keys.Select(id => id.Value));
    }

    [Fact]
    public void WiresEnumerateInAscendingIdentifierOrder()
    {
        var schematic = Schematic.Empty;

        for (var index = 0; index < 5; index++)
        {
            schematic = new AddWire(new GridPoint(0, index), new GridPoint(4, index))
                .Succeed(schematic);
        }

        schematic = new RemoveWire(new WireId(2)).Succeed(schematic);
        schematic = new AddWire(new GridPoint(0, 99), new GridPoint(4, 99)).Succeed(schematic);

        Assert.Equal([1, 3, 4, 5, 6], schematic.Wires.Keys.Select(id => id.Value));
    }

    [Fact]
    public void AKnownComponentIsFoundByItsIdentifier()
    {
        var id = Schematic.Empty.NextComponentId;
        var schematic = new AddComponent("resistor", new GridPoint(2, 3), Orientation.Default)
            .Succeed(Schematic.Empty);

        Assert.True(schematic.TryGetComponent(id, out var component));
        Assert.Equal("resistor", component.DefinitionId);
        Assert.Equal(new GridPoint(2, 3), component.Position);
    }

    [Fact]
    public void AnUnknownComponentIsNotFound()
    {
        Assert.False(Schematic.Empty.TryGetComponent(new ComponentId(7), out var component));
        Assert.Null(component);
    }

    [Fact]
    public void AKnownWireIsFoundByItsIdentifier()
    {
        var id = Schematic.Empty.NextWireId;
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(0, 6)).Succeed(Schematic.Empty);

        Assert.True(schematic.TryGetWire(id, out var wire));
        Assert.Equal(new GridPoint(0, 6), wire.End);
    }

    [Fact]
    public void AnUnknownWireIsNotFound()
    {
        Assert.False(Schematic.Empty.TryGetWire(new WireId(7), out var wire));
        Assert.Null(wire);
    }

    [Fact]
    public void PinPositionsFollowTheDefinitionPinOrder()
    {
        var id = Schematic.Empty.NextComponentId;
        var schematic = new AddComponent("npn", new GridPoint(0, 0), Orientation.Default)
            .Succeed(Schematic.Empty);

        Assert.True(schematic.TryGetPinPositions(id, BaseCatalog.Catalog, out var positions));
        Assert.Equal(["collector", "base", "emitter"], positions.Select(pin => pin.PinName));
    }

    [Fact]
    public void PinPositionsOfAnUnknownComponentAreNotReturned()
    {
        Assert.False(
            Schematic.Empty.TryGetPinPositions(new ComponentId(3), BaseCatalog.Catalog, out var positions));
        Assert.True(positions.IsDefault);
    }

    [Fact]
    public void PinPositionsOfAnUnknownDefinitionAreNotReturned()
    {
        var id = Schematic.Empty.NextComponentId;
        var schematic = new AddComponent("widget", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty, WidgetCatalog());

        Assert.False(schematic.TryGetPinPositions(id, ComponentCatalog.Empty, out var positions));
        Assert.True(positions.IsDefault);
    }

    [Fact]
    public void PinPositionsNeedACatalog()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => Schematic.Empty.TryGetPinPositions(ComponentId.First, null!, out _));

        Assert.Equal("catalog", exception.ParamName);
    }

    private static ComponentCatalog WidgetCatalog() => new([
        new ComponentDefinition(
            "widget",
            "Widget",
            "A widget.",
            ComponentCategory.Passive,
            "W",
            [new PinDefinition("1", -1, 0)],
            [new ParameterDefinition("gain", Unit.Dimensionless, new Quantity(2, Unit.Dimensionless), null)]),
    ]);
}
