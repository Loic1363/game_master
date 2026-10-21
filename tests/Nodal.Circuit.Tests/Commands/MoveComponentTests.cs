using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class MoveComponentTests
{
    [Fact]
    public void MovingChangesOnlyThePosition()
    {
        var schematic = Placed(out var id);

        var moved = new MoveComponent(id, new GridPoint(-7, 12)).Succeed(schematic);

        Assert.True(moved.TryGetComponent(id, out var component));
        Assert.Equal(new GridPoint(-7, 12), component.Position);
        Assert.Equal(new Orientation(Rotation.R180, false), component.Orientation);
        Assert.Equal("R1", component.Designator);
    }

    [Fact]
    public void MovingToTheSamePositionIsStillASuccess()
    {
        var schematic = Placed(out var id);

        var moved = new MoveComponent(id, new GridPoint(3, 4)).Succeed(schematic);

        Assert.True(moved.TryGetComponent(id, out var component));
        Assert.Equal(new GridPoint(3, 4), component.Position);
    }

    [Fact]
    public void MovingDragsThePinsAlong()
    {
        var schematic = Placed(out var id);

        var moved = new MoveComponent(id, new GridPoint(0, 0)).Succeed(schematic);

        Assert.True(moved.TryGetPinPositions(id, BaseCatalog.Catalog, out var positions));
        Assert.Equal(
            [new PinPosition("1", new GridPoint(2, 0)), new PinPosition("2", new GridPoint(-2, 0))],
            positions);
    }

    [Fact]
    public void MovingAnUnknownComponentFails()
    {
        var error = new MoveComponent(new ComponentId(4), GridPoint.Origin).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
    }

    [Fact]
    public void TheDescriptionNamesTheDesignator()
    {
        var schematic = Placed(out var id);

        Assert.Equal("Move R1", new MoveComponent(id, GridPoint.Origin).DescriptionOf(schematic));
    }

    private static Schematic Placed(out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("resistor", new GridPoint(3, 4), new Orientation(Rotation.R180, false))
            .Succeed(Schematic.Empty);
    }
}
