using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class RotateComponentTests
{
    [Fact]
    public void RotatingAdvancesOneClockwiseQuarterTurn()
    {
        var schematic = Placed(Orientation.Default, out var id);

        var rotated = new RotateComponent(id).Succeed(schematic);

        Assert.True(rotated.TryGetComponent(id, out var component));
        Assert.Equal(new Orientation(Rotation.R90, false), component.Orientation);
    }

    [Fact]
    public void RotatingKeepsTheMirrorFlagAndThePosition()
    {
        var schematic = Placed(new Orientation(Rotation.R270, true), out var id);

        var rotated = new RotateComponent(id).Succeed(schematic);

        Assert.True(rotated.TryGetComponent(id, out var component));
        Assert.Equal(new Orientation(Rotation.R0, true), component.Orientation);
        Assert.Equal(new GridPoint(5, 5), component.Position);
    }

    [Fact]
    public void FourRotationsReturnToTheStart()
    {
        var schematic = Placed(new Orientation(Rotation.R90, true), out var id);
        var rotated = schematic;

        for (var turn = 0; turn < 4; turn++)
        {
            rotated = new RotateComponent(id).Succeed(rotated);
        }

        Assert.True(rotated.TryGetComponent(id, out var component));
        Assert.Equal(new Orientation(Rotation.R90, true), component.Orientation);
    }

    [Fact]
    public void RotatingTurnsThePinsAroundThePosition()
    {
        var schematic = Placed(Orientation.Default, out var id);

        var rotated = new RotateComponent(id).Succeed(schematic);

        Assert.True(rotated.TryGetPinPositions(id, BaseCatalog.Catalog, out var positions));
        Assert.Equal(
            [new PinPosition("1", new GridPoint(5, 3)), new PinPosition("2", new GridPoint(5, 7))],
            positions);
    }

    [Fact]
    public void RotatingAnUnknownComponentFails()
    {
        var error = new RotateComponent(new ComponentId(2)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
    }

    [Fact]
    public void TheDescriptionNamesTheDesignator()
    {
        var schematic = new AddComponent("npn", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);
        schematic = new AddComponent("pnp", new GridPoint(9, 0), Orientation.Default).Succeed(schematic);

        Assert.Equal("Rotate Q2", new RotateComponent(new ComponentId(2)).DescriptionOf(schematic));
    }

    private static Schematic Placed(Orientation orientation, out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("resistor", new GridPoint(5, 5), orientation).Succeed(Schematic.Empty);
    }
}
