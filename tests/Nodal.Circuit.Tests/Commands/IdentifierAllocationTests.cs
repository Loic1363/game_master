using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class IdentifierAllocationTests
{
    [Fact]
    public void AComponentIdentifierIsNeverReusedAfterARemoval()
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);
        schematic = new RemoveComponent(ComponentId.First).Succeed(schematic);

        schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(schematic);

        Assert.Equal(new ComponentId(2), Assert.Single(schematic.Components.Values).Id);
        Assert.Equal(new ComponentId(3), schematic.NextComponentId);
    }

    [Fact]
    public void AWireIdentifierIsNeverReusedAfterARemoval()
    {
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);
        schematic = new RemoveWire(WireId.First).Succeed(schematic);

        schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(schematic);

        Assert.Equal(new WireId(2), Assert.Single(schematic.Wires.Values).Id);
        Assert.Equal(new WireId(3), schematic.NextWireId);
    }

    [Fact]
    public void IdentifiersKeepClimbingThroughManyRemovals()
    {
        var schematic = Schematic.Empty;

        for (var round = 1; round <= 4; round++)
        {
            schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
                .Succeed(schematic);
            schematic = new RemoveComponent(new ComponentId(round)).Succeed(schematic);
        }

        Assert.Empty(schematic.Components);
        Assert.Equal(new ComponentId(5), schematic.NextComponentId);
    }

    [Fact]
    public void ComponentAndWireIdentifiersAdvanceIndependently()
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);
        schematic = new AddComponent("capacitor", new GridPoint(8, 0), Orientation.Default)
            .Succeed(schematic);
        schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(schematic);

        Assert.Equal(new ComponentId(3), schematic.NextComponentId);
        Assert.Equal(new WireId(2), schematic.NextWireId);
    }
}
