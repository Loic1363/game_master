using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Geometry;

public class OrientationTests
{
    private static readonly GridPoint Placement = new(10, 20);

    [Fact]
    public void DefaultIsUnrotatedAndNotMirrored()
    {
        Assert.Equal(Rotation.R0, Orientation.Default.Rotation);
        Assert.False(Orientation.Default.Mirrored);
        Assert.Equal(Orientation.Default, default);
    }

    public static TheoryData<Rotation, bool, int, int, int, int> ResistorPins => new()
    {
        { Rotation.R0, false, 8, 20, 12, 20 },
        { Rotation.R90, false, 10, 18, 10, 22 },
        { Rotation.R180, false, 12, 20, 8, 20 },
        { Rotation.R270, false, 10, 22, 10, 18 },
        { Rotation.R0, true, 12, 20, 8, 20 },
        { Rotation.R90, true, 10, 22, 10, 18 },
        { Rotation.R180, true, 8, 20, 12, 20 },
        { Rotation.R270, true, 10, 18, 10, 22 },
    };

    [Theory]
    [MemberData(nameof(ResistorPins))]
    public void ResistorPinsLandOnTheirAbsolutePositions(
        Rotation rotation, bool mirrored, int firstX, int firstY, int secondX, int secondY) =>
        AssertPinPositions(
            "resistor",
            new Orientation(rotation, mirrored),
            new PinPosition("1", new GridPoint(firstX, firstY)),
            new PinPosition("2", new GridPoint(secondX, secondY)));

    public static TheoryData<Rotation, bool, int, int, int, int, int, int> NpnPins => new()
    {
        { Rotation.R0, false, 10, 18, 8, 20, 10, 22 },
        { Rotation.R90, false, 12, 20, 10, 18, 8, 20 },
        { Rotation.R180, false, 10, 22, 12, 20, 10, 18 },
        { Rotation.R270, false, 8, 20, 10, 22, 12, 20 },
        { Rotation.R0, true, 10, 18, 12, 20, 10, 22 },
        { Rotation.R90, true, 12, 20, 10, 22, 8, 20 },
        { Rotation.R180, true, 10, 22, 8, 20, 10, 18 },
        { Rotation.R270, true, 8, 20, 10, 18, 12, 20 },
    };

    [Theory]
    [MemberData(nameof(NpnPins))]
    public void NpnPinsLandOnTheirAbsolutePositions(
        Rotation rotation,
        bool mirrored,
        int collectorX,
        int collectorY,
        int baseX,
        int baseY,
        int emitterX,
        int emitterY) =>
        AssertPinPositions(
            "npn",
            new Orientation(rotation, mirrored),
            new PinPosition("collector", new GridPoint(collectorX, collectorY)),
            new PinPosition("base", new GridPoint(baseX, baseY)),
            new PinPosition("emitter", new GridPoint(emitterX, emitterY)));

    [Theory]
    [InlineData(Rotation.R0, Rotation.R90)]
    [InlineData(Rotation.R90, Rotation.R180)]
    [InlineData(Rotation.R180, Rotation.R270)]
    [InlineData(Rotation.R270, Rotation.R0)]
    public void RotatingClockwiseAdvancesOneQuarterTurn(Rotation from, Rotation to)
    {
        var rotated = new Orientation(from, false).RotatedClockwise();

        Assert.Equal(new Orientation(to, false), rotated);
    }

    [Fact]
    public void FourClockwiseQuarterTurnsReturnToTheStart()
    {
        var orientation = new Orientation(Rotation.R90, true);
        var turned = orientation;

        for (var turn = 0; turn < 4; turn++)
        {
            turned = turned.RotatedClockwise();
        }

        Assert.Equal(orientation, turned);
    }

    [Fact]
    public void RotatingClockwiseKeepsTheMirrorFlag()
    {
        Assert.True(new Orientation(Rotation.R0, true).RotatedClockwise().Mirrored);
        Assert.False(new Orientation(Rotation.R0, false).RotatedClockwise().Mirrored);
    }

    [Fact]
    public void FlippingTogglesTheMirrorFlagAndKeepsTheRotation()
    {
        var orientation = new Orientation(Rotation.R180, false);

        Assert.Equal(new Orientation(Rotation.R180, true), orientation.Flipped());
        Assert.Equal(orientation, orientation.Flipped().Flipped());
    }

    [Fact]
    public void OneQuarterTurnSendsRightToDown() =>
        Assert.Equal(
            new GridPoint(0, 1), new Orientation(Rotation.R90, false).Transform(new GridPoint(1, 0)));

    [Fact]
    public void MirroringHappensBeforeRotating() =>
        Assert.Equal(
            new GridPoint(-1, -3), new Orientation(Rotation.R90, true).Transform(new GridPoint(3, 1)));

    [Theory]
    [InlineData(Rotation.R0, false, "R0")]
    [InlineData(Rotation.R270, false, "R270")]
    [InlineData(Rotation.R90, true, "R90 mirrored")]
    public void TextNamesTheRotationAndTheMirror(Rotation rotation, bool mirrored, string expected) =>
        Assert.Equal(expected, new Orientation(rotation, mirrored).ToString());

    private static void AssertPinPositions(
        string definitionId, Orientation orientation, params PinPosition[] expected)
    {
        var id = Schematic.Empty.NextComponentId;
        var schematic = new AddComponent(definitionId, Placement, orientation).Succeed(Schematic.Empty);

        Assert.True(schematic.TryGetPinPositions(id, BaseCatalog.Catalog, out var positions));
        Assert.Equal(expected, positions);
    }
}
