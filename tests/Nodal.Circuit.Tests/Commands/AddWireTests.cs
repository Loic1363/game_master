using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class AddWireTests
{
    [Fact]
    public void AddingAHorizontalWireKeepsItsEndpointsAndAdvancesTheNextIdentifier()
    {
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);

        var wire = Assert.Single(schematic.Wires.Values);
        Assert.Equal(WireId.First, wire.Id);
        Assert.Equal(new GridPoint(0, 0), wire.Start);
        Assert.Equal(new GridPoint(6, 0), wire.End);
        Assert.True(wire.IsHorizontal);
        Assert.Equal(new WireId(2), schematic.NextWireId);
    }

    [Fact]
    public void AddingAVerticalWireIsAccepted()
    {
        var schematic = new AddWire(new GridPoint(2, 9), new GridPoint(2, 3)).Succeed(Schematic.Empty);

        Assert.False(Assert.Single(schematic.Wires.Values).IsHorizontal);
    }

    [Fact]
    public void AddingAWireDoesNotTouchTheComponents()
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        var withWire = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(schematic);

        Assert.Single(withWire.Components);
        Assert.Equal(schematic.NextComponentId, withWire.NextComponentId);
    }

    [Fact]
    public void AZeroLengthWireFails()
    {
        var error = new AddWire(new GridPoint(3, 3), new GridPoint(3, 3)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.ZeroLengthWire, error.Code);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(0, 0, -4, 2)]
    [InlineData(5, 5, 2, 8)]
    public void ADiagonalWireFails(int startX, int startY, int endX, int endY)
    {
        var error = new AddWire(new GridPoint(startX, startY), new GridPoint(endX, endY))
            .Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.DiagonalWire, error.Code);
    }

    [Fact]
    public void AnIdenticalWireFails()
    {
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);

        var error = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Reject(schematic);

        Assert.Equal(EditErrorCode.DuplicateWire, error.Code);
    }

    [Fact]
    public void AReversedIdenticalWireFails()
    {
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);

        var error = new AddWire(new GridPoint(6, 0), new GridPoint(0, 0)).Reject(schematic);

        Assert.Equal(EditErrorCode.DuplicateWire, error.Code);
    }

    [Fact]
    public void AWireThatOnlySharesOneEndpointIsAccepted()
    {
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);

        var bent = new AddWire(new GridPoint(6, 0), new GridPoint(6, 4)).Succeed(schematic);

        Assert.Equal(2, bent.Wires.Count);
    }

    [Fact]
    public void ADuplicateIsStillRejectedAfterTheOriginalMovesInTheCollection()
    {
        var schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);
        schematic = new AddWire(new GridPoint(0, 1), new GridPoint(6, 1)).Succeed(schematic);
        schematic = new RemoveWire(WireId.First).Succeed(schematic);

        var error = new AddWire(new GridPoint(6, 1), new GridPoint(0, 1)).Reject(schematic);

        Assert.Equal(EditErrorCode.DuplicateWire, error.Code);
    }

    [Fact]
    public void TheDescriptionIsTheSameForEveryWire() =>
        Assert.Equal(
            "Add wire",
            new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).DescriptionOf(Schematic.Empty));
}
