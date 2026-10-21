using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class DesignatorTests
{
    [Fact]
    public void DesignatorsOfOnePrefixCountUp()
    {
        var schematic = AddAll("resistor", "resistor", "resistor");

        Assert.Equal(["R1", "R2", "R3"], Designators(schematic));
    }

    [Fact]
    public void TheSmallestFreeNumberFillsAGap()
    {
        var schematic = AddAll("resistor", "resistor", "resistor");
        schematic = new RemoveComponent(new ComponentId(2)).Succeed(schematic);

        schematic = Add(schematic, "resistor");

        Assert.Equal(["R1", "R3", "R2"], Designators(schematic));
    }

    [Fact]
    public void PrefixesCountIndependently()
    {
        var schematic = AddAll("resistor", "capacitor", "resistor", "inductor", "capacitor");

        Assert.Equal(["R1", "C1", "R2", "L1", "C2"], Designators(schematic));
    }

    [Fact]
    public void DefinitionsThatShareAPrefixShareOneCounter()
    {
        var schematic = AddAll("npn", "pnp", "npn");

        Assert.Equal(["Q1", "Q2", "Q3"], Designators(schematic));
    }

    [Fact]
    public void ANumberIsReusedAfterItsComponentIsRemoved()
    {
        var schematic = AddAll("resistor", "resistor");
        schematic = new RemoveComponent(new ComponentId(1)).Succeed(schematic);

        schematic = Add(schematic, "resistor");

        Assert.Equal(["R2", "R1"], Designators(schematic));
    }

    [Fact]
    public void RemovingEveryComponentRestartsAtOne()
    {
        var schematic = AddAll("resistor", "resistor");
        schematic = new RemoveComponent(new ComponentId(1)).Succeed(schematic);
        schematic = new RemoveComponent(new ComponentId(2)).Succeed(schematic);

        schematic = Add(schematic, "resistor");

        Assert.Equal(["R1"], Designators(schematic));
    }

    [Fact]
    public void AMultiLetterPrefixIsNumberedLikeAnyOther()
    {
        var schematic = AddAll("ground", "ground");

        Assert.Equal(["GND1", "GND2"], Designators(schematic));
    }

    private static Schematic AddAll(params string[] definitionIds)
    {
        var schematic = Schematic.Empty;

        foreach (var definitionId in definitionIds)
        {
            schematic = Add(schematic, definitionId);
        }

        return schematic;
    }

    private static Schematic Add(Schematic schematic, string definitionId) =>
        new AddComponent(definitionId, GridPoint.Origin, Orientation.Default).Succeed(schematic);

    private static IEnumerable<string> Designators(Schematic schematic) =>
        schematic.Components.Values.Select(component => component.Designator);
}
