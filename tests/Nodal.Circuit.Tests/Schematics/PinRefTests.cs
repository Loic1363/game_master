using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Xunit;

namespace Nodal.Circuit.Tests.Schematics;

public class PinRefTests
{
    [Fact]
    public void APinReferenceNamesItsComponentAndPin()
    {
        var pin = new PinRef(new ComponentId(3), "base");

        Assert.Equal(new ComponentId(3), pin.ComponentId);
        Assert.Equal("base", pin.PinName);
        Assert.Equal("3.base", pin.ToString());
    }

    [Fact]
    public void PinReferencesWithTheSameComponentAndPinAreEqual()
    {
        Assert.Equal(new PinRef(new ComponentId(1), "a"), new PinRef(new ComponentId(1), "a"));
        Assert.NotEqual(new PinRef(new ComponentId(1), "a"), new PinRef(new ComponentId(1), "b"));
        Assert.NotEqual(new PinRef(new ComponentId(1), "a"), new PinRef(new ComponentId(2), "a"));
    }

    [Fact]
    public void TheContextFreeOrderComparesTheComponentFirst()
    {
        Assert.True(new PinRef(new ComponentId(1), "z").CompareTo(new PinRef(new ComponentId(2), "a")) < 0);
        Assert.True(new PinRef(new ComponentId(2), "a").CompareTo(new PinRef(new ComponentId(1), "z")) > 0);
    }

    [Fact]
    public void TheContextFreeOrderComparesPinNamesOrdinally()
    {
        var component = new ComponentId(1);

        Assert.True(new PinRef(component, "base").CompareTo(new PinRef(component, "collector")) < 0);
        Assert.Equal(0, new PinRef(component, "base").CompareTo(new PinRef(component, "base")));
    }

    [Fact]
    public void TheDefinitionOrderComparesTheComponentFirst()
    {
        var schematic = Schematic.Empty.Add("npn").Add("npn");
        var order = PinRefOrder.For(schematic, BaseCatalog.Catalog);

        Assert.True(order.Compare(
            new PinRef(new ComponentId(1), "emitter"), new PinRef(new ComponentId(2), "collector")) < 0);
    }

    [Fact]
    public void TheDefinitionOrderFollowsTheDeclaredPinOrder()
    {
        var schematic = Schematic.Empty.Add("npn");
        var order = PinRefOrder.For(schematic, BaseCatalog.Catalog);
        var component = new ComponentId(1);

        var sorted = new[]
            {
                new PinRef(component, "emitter"),
                new PinRef(component, "base"),
                new PinRef(component, "collector"),
            }
            .Order(order)
            .Select(pin => pin.PinName);

        Assert.Equal(["collector", "base", "emitter"], sorted);
    }

    [Fact]
    public void TheTwoOrdersDisagreeWhenTheDefinitionOrderIsNotAlphabetical()
    {
        var schematic = Schematic.Empty.Add("npn");
        var order = PinRefOrder.For(schematic, BaseCatalog.Catalog);
        var component = new ComponentId(1);
        var collector = new PinRef(component, "collector");
        var npnBase = new PinRef(component, "base");

        Assert.True(order.Compare(collector, npnBase) < 0);
        Assert.True(collector.CompareTo(npnBase) > 0);
    }

    [Fact]
    public void AnUnknownPinSortsAfterTheDeclaredOnes()
    {
        var schematic = Schematic.Empty.Add("npn");
        var order = PinRefOrder.For(schematic, BaseCatalog.Catalog);
        var component = new ComponentId(1);

        Assert.True(order.Compare(new PinRef(component, "emitter"), new PinRef(component, "drain")) < 0);
    }

    [Fact]
    public void TwoUnknownPinsFallBackToTheirNames()
    {
        var order = PinRefOrder.For(Schematic.Empty, BaseCatalog.Catalog);
        var component = new ComponentId(1);

        Assert.True(order.Compare(new PinRef(component, "a"), new PinRef(component, "b")) < 0);
        Assert.Equal(0, order.Compare(new PinRef(component, "a"), new PinRef(component, "a")));
    }

    [Fact]
    public void AComponentWithAnUnknownDefinitionContributesNoPinOrder()
    {
        var schematic = Schematic.Empty.Add("npn");
        var order = PinRefOrder.For(schematic, ComponentCatalog.Empty);
        var component = new ComponentId(1);

        Assert.True(order.Compare(new PinRef(component, "base"), new PinRef(component, "collector")) < 0);
    }

    [Fact]
    public void BuildingAnOrderNeedsASchematicAndACatalog()
    {
        Assert.Equal(
            "schematic",
            Assert.Throws<ArgumentNullException>(
                () => PinRefOrder.For(null!, BaseCatalog.Catalog)).ParamName);
        Assert.Equal(
            "catalog",
            Assert.Throws<ArgumentNullException>(
                () => PinRefOrder.For(Schematic.Empty, null!)).ParamName);
    }
}

internal static class PinRefTestSchematics
{
    public static Schematic Add(this Schematic schematic, string definitionId) =>
        new AddComponent(definitionId, GridPoint.Origin, Orientation.Default).Succeed(schematic);
}
