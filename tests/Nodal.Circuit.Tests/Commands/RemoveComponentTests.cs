using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class RemoveComponentTests
{
    [Fact]
    public void RemovingDropsTheComponent()
    {
        var schematic = Placed(out var id);

        var without = new RemoveComponent(id).Succeed(schematic);

        Assert.Empty(without.Components);
    }

    [Fact]
    public void RemovingKeepsTheOtherComponents()
    {
        var schematic = Placed(out var id);
        schematic = new AddComponent("capacitor", new GridPoint(8, 0), Orientation.Default)
            .Succeed(schematic);

        var without = new RemoveComponent(id).Succeed(schematic);

        Assert.Equal("capacitor", Assert.Single(without.Components.Values).DefinitionId);
    }

    [Fact]
    public void RemovingNeverLowersTheNextIdentifier()
    {
        var schematic = Placed(out var id);

        var without = new RemoveComponent(id).Succeed(schematic);

        Assert.Equal(schematic.NextComponentId, without.NextComponentId);
    }

    [Fact]
    public void RemovingAnUnknownComponentFails()
    {
        var error = new RemoveComponent(new ComponentId(9)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
        Assert.Contains("9", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDescriptionNamesTheDesignator()
    {
        var schematic = Placed(out var id);

        Assert.Equal("Remove R1", new RemoveComponent(id).DescriptionOf(schematic));
    }

    private static Schematic Placed(out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);
    }
}
