using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests;

public class ImmutabilityTests
{
    public static TheoryData<IEditCommand> Commands => new()
    {
        new AddComponent("capacitor", new GridPoint(20, 20), Orientation.Default),
        new RemoveComponent(ComponentId.First),
        new MoveComponent(ComponentId.First, new GridPoint(30, 30)),
        new RotateComponent(ComponentId.First),
        new MirrorComponent(ComponentId.First),
        new SetParameter(ComponentId.First, "resistance", new Quantity(4700, Unit.Ohm)),
        new AddWire(new GridPoint(40, 0), new GridPoint(40, 8)),
        new RemoveWire(WireId.First),
        new CompositeCommand("Rotate twice", [
            new RotateComponent(ComponentId.First),
            new RotateComponent(ComponentId.First),
        ]),
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public void ASnapshotTakenBeforeACommandIsUnchangedAfterIt(IEditCommand command)
    {
        var snapshot = Populated();
        var components = snapshot.Components;
        var wires = snapshot.Wires;

        command.Succeed(snapshot);

        Assert.Equal(2, snapshot.Components.Count);
        Assert.Equal(2, snapshot.Wires.Count);
        Assert.Same(components, snapshot.Components);
        Assert.Same(wires, snapshot.Wires);
        Assert.Equal(new ComponentId(3), snapshot.NextComponentId);
        Assert.Equal(new WireId(3), snapshot.NextWireId);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void ACommandNeverEditsTheComponentItReads(IEditCommand command)
    {
        var snapshot = Populated();
        Assert.True(snapshot.TryGetComponent(ComponentId.First, out var before));

        command.Succeed(snapshot);

        Assert.True(snapshot.TryGetComponent(ComponentId.First, out var after));
        Assert.Same(before, after);
        Assert.Equal("R1", after.Designator);
        Assert.Equal(new GridPoint(1, 2), after.Position);
        Assert.Equal(Orientation.Default, after.Orientation);
        Assert.Equal(new Quantity(1000, Unit.Ohm), after.Parameters["resistance"]);
    }

    [Fact]
    public void TheEmptySchematicStaysEmpty()
    {
        new AddComponent("resistor", GridPoint.Origin, Orientation.Default).Succeed(Schematic.Empty);
        new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(Schematic.Empty);

        Assert.Empty(Schematic.Empty.Components);
        Assert.Empty(Schematic.Empty.Wires);
        Assert.Equal(ComponentId.First, Schematic.Empty.NextComponentId);
        Assert.Equal(WireId.First, Schematic.Empty.NextWireId);
    }

    [Fact]
    public void AppliedCommandsReturnADifferentSchematic()
    {
        var snapshot = Populated();

        var moved = new MoveComponent(ComponentId.First, new GridPoint(9, 9)).Succeed(snapshot);

        Assert.NotSame(snapshot, moved);
    }

    private static Schematic Populated()
    {
        var schematic = new AddComponent("resistor", new GridPoint(1, 2), Orientation.Default)
            .Succeed(Schematic.Empty);
        schematic = new AddComponent("npn", new GridPoint(10, 2), Orientation.Default)
            .Succeed(schematic);
        schematic = new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)).Succeed(schematic);

        return new AddWire(new GridPoint(6, 0), new GridPoint(6, 4)).Succeed(schematic);
    }
}
