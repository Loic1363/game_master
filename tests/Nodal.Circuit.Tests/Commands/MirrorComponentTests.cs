using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class MirrorComponentTests
{
    [Fact]
    public void MirroringSetsTheMirrorFlag()
    {
        var schematic = Placed(Orientation.Default, out var id);

        var mirrored = new MirrorComponent(id).Succeed(schematic);

        Assert.True(mirrored.TryGetComponent(id, out var component));
        Assert.Equal(new Orientation(Rotation.R0, true), component.Orientation);
    }

    [Fact]
    public void MirroringTwiceClearsTheMirrorFlag()
    {
        var schematic = Placed(Orientation.Default, out var id);

        var mirrored = new MirrorComponent(id).Succeed(schematic);
        mirrored = new MirrorComponent(id).Succeed(mirrored);

        Assert.True(mirrored.TryGetComponent(id, out var component));
        Assert.Equal(Orientation.Default, component.Orientation);
    }

    [Fact]
    public void MirroringKeepsTheRotationAndThePosition()
    {
        var schematic = Placed(new Orientation(Rotation.R270, false), out var id);

        var mirrored = new MirrorComponent(id).Succeed(schematic);

        Assert.True(mirrored.TryGetComponent(id, out var component));
        Assert.Equal(new Orientation(Rotation.R270, true), component.Orientation);
        Assert.Equal(new GridPoint(5, 5), component.Position);
    }

    [Fact]
    public void MirroringFlipsThePinsAroundThePosition()
    {
        var schematic = Placed(Orientation.Default, out var id);

        var mirrored = new MirrorComponent(id).Succeed(schematic);

        Assert.True(mirrored.TryGetPinPositions(id, BaseCatalog.Catalog, out var positions));
        Assert.Equal(
            [new PinPosition("1", new GridPoint(7, 5)), new PinPosition("2", new GridPoint(3, 5))],
            positions);
    }

    [Fact]
    public void MirroringAnUnknownComponentFails()
    {
        var error = new MirrorComponent(new ComponentId(2)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
    }

    [Fact]
    public void TheDescriptionNamesTheDesignator()
    {
        var schematic = Placed(Orientation.Default, out var id);

        Assert.Equal("Mirror R1", new MirrorComponent(id).DescriptionOf(schematic));
    }

    private static Schematic Placed(Orientation orientation, out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("resistor", new GridPoint(5, 5), orientation).Succeed(Schematic.Empty);
    }
}
