using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class RemoveWireTests
{
    [Fact]
    public void RemovingDropsTheWire()
    {
        var schematic = Wired(out var id);

        var without = new RemoveWire(id).Succeed(schematic);

        Assert.Empty(without.Wires);
    }

    [Fact]
    public void RemovingKeepsTheOtherWires()
    {
        var schematic = Wired(out var id);
        schematic = new AddWire(new GridPoint(6, 0), new GridPoint(6, 4)).Succeed(schematic);

        var without = new RemoveWire(id).Succeed(schematic);

        Assert.Equal(new GridPoint(6, 4), Assert.Single(without.Wires.Values).End);
    }

    [Fact]
    public void RemovingNeverLowersTheNextIdentifier()
    {
        var schematic = Wired(out var id);

        var without = new RemoveWire(id).Succeed(schematic);

        Assert.Equal(schematic.NextWireId, without.NextWireId);
    }

    [Fact]
    public void RemovingAnUnknownWireFails()
    {
        var error = new RemoveWire(new WireId(5)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownWire, error.Code);
        Assert.Contains("5", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovingTheSameWireTwiceFailsTheSecondTime()
    {
        var schematic = Wired(out var id);
        var without = new RemoveWire(id).Succeed(schematic);

        Assert.Equal(EditErrorCode.UnknownWire, new RemoveWire(id).Reject(without).Code);
    }

    [Fact]
    public void TheDescriptionIsTheSameForEveryWire()
    {
        var schematic = Wired(out var id);

        Assert.Equal("Remove wire", new RemoveWire(id).DescriptionOf(schematic));
    }

    private static Schematic Wired(out WireId id)
    {
        id = Schematic.Empty.NextWireId;

        return new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);
    }
}
