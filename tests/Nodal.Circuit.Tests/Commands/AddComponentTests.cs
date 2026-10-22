using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class AddComponentTests
{
    [Fact]
    public void AddingPlacesTheComponentAndAdvancesTheNextIdentifier()
    {
        var command = new AddComponent("resistor", new GridPoint(4, -2), new Orientation(Rotation.R90, true));

        var schematic = command.Succeed(Schematic.Empty);

        var component = Assert.Single(schematic.Components.Values);
        Assert.Equal(ComponentId.First, component.Id);
        Assert.Equal("resistor", component.DefinitionId);
        Assert.Equal("R1", component.Designator);
        Assert.Equal(new GridPoint(4, -2), component.Position);
        Assert.Equal(new Orientation(Rotation.R90, true), component.Orientation);
        Assert.Equal(new ComponentId(2), schematic.NextComponentId);
    }

    [Fact]
    public void AddingSeedsEveryRequiredParameterWithItsDefault()
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        var component = Assert.Single(schematic.Components.Values);
        var parameter = Assert.Single(component.Parameters);
        Assert.Equal("resistance", parameter.Key);
        Assert.Equal(new Quantity(1000, Unit.Ohm), parameter.Value);
    }

    [Fact]
    public void AddingLeavesOptionalParametersUnset()
    {
        var schematic = new AddComponent("capacitor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        var component = Assert.Single(schematic.Components.Values);

        Assert.True(BaseCatalog.Catalog.TryGetById("capacitor", out var definition));
        Assert.Equal(7, definition.Parameters.Length);
        Assert.Equal(["capacitance"], component.Parameters.Keys);
    }

    [Fact]
    public void AddingAComponentWithoutParametersLeavesNoParameters()
    {
        var schematic = new AddComponent("npn", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        Assert.Empty(Assert.Single(schematic.Components.Values).Parameters);
    }

    [Fact]
    public void AddingDoesNotTouchTheWires()
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        Assert.Empty(schematic.Wires);
        Assert.Equal(WireId.First, schematic.NextWireId);
    }

    [Fact]
    public void AddingAnUnknownDefinitionFails()
    {
        var error = new AddComponent("flux-capacitor", GridPoint.Origin, Orientation.Default)
            .Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownDefinition, error.Code);
        Assert.Contains("flux-capacitor", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddingToAnEmptyCatalogFails()
    {
        var error = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Reject(Schematic.Empty, ComponentCatalog.Empty);

        Assert.Equal(EditErrorCode.UnknownDefinition, error.Code);
    }

    [Fact]
    public void TheDescriptionNamesTheAllocatedDesignator()
    {
        var command = new AddComponent("npn", GridPoint.Origin, Orientation.Default);

        Assert.Equal("Add Q1", command.DescriptionOf(Schematic.Empty));
    }

    [Fact]
    public void AddingNeedsASchematicAndACatalog()
    {
        var command = new AddComponent("resistor", GridPoint.Origin, Orientation.Default);

        Assert.Equal(
            "schematic",
            Assert.Throws<ArgumentNullException>(() => command.Apply(null!, BaseCatalog.Catalog)).ParamName);
        Assert.Equal(
            "catalog",
            Assert.Throws<ArgumentNullException>(() => command.Apply(Schematic.Empty, null!)).ParamName);
    }
}
