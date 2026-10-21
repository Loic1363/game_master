using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class CompositeCommandTests
{
    [Fact]
    public void EveryStepIsAppliedInOrder()
    {
        var command = new CompositeCommand("Place a divider", [
            new AddComponent("resistor", new GridPoint(0, 0), Orientation.Default),
            new AddComponent("resistor", new GridPoint(0, 8), Orientation.Default),
            new AddWire(new GridPoint(2, 0), new GridPoint(2, 8)),
        ]);

        var schematic = command.Succeed(Schematic.Empty);

        Assert.Equal(["R1", "R2"], schematic.Components.Values.Select(component => component.Designator));
        Assert.Single(schematic.Wires);
    }

    [Fact]
    public void LaterStepsSeeWhatEarlierStepsDid()
    {
        var id = Schematic.Empty.NextComponentId;
        var command = new CompositeCommand("Place a resistor of 4.7 k", [
            new AddComponent("resistor", GridPoint.Origin, Orientation.Default),
            new SetParameter(id, "resistance", new Quantity(4700, Unit.Ohm)),
            new RotateComponent(id),
        ]);

        var schematic = command.Succeed(Schematic.Empty);

        Assert.True(schematic.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(4700, Unit.Ohm), component.Parameters["resistance"]);
        Assert.Equal(new Orientation(Rotation.R90, false), component.Orientation);
    }

    [Fact]
    public void AFailingThirdStepLeavesTheSchematicUnchanged()
    {
        var command = new CompositeCommand("Place a divider", [
            new AddComponent("resistor", new GridPoint(0, 0), Orientation.Default),
            new AddComponent("resistor", new GridPoint(0, 8), Orientation.Default),
            new RemoveComponent(new ComponentId(99)),
        ]);

        var error = command.Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
        Assert.Empty(Schematic.Empty.Components);
        Assert.Equal(ComponentId.First, Schematic.Empty.NextComponentId);
    }

    [Fact]
    public void AFailingFirstStepStopsTheWholeCommand()
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        var error = new CompositeCommand("Replace everything", [
            new RemoveComponent(new ComponentId(99)),
            new AddComponent("capacitor", GridPoint.Origin, Orientation.Default),
        ]).Reject(schematic);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
        Assert.Single(schematic.Components);
    }

    [Fact]
    public void TheFailureOfTheFailingStepIsReported()
    {
        var error = new CompositeCommand("Draw a corner", [
            new AddWire(new GridPoint(0, 0), new GridPoint(6, 0)),
            new AddWire(new GridPoint(6, 0), new GridPoint(9, 3)),
        ]).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.DiagonalWire, error.Code);
    }

    [Fact]
    public void AnEmptyCompositeSucceedsAndChangesNothing()
    {
        var schematic = new CompositeCommand("Do nothing", []).Succeed(Schematic.Empty);

        Assert.Same(Schematic.Empty, schematic);
    }

    [Fact]
    public void TheDescriptionIsTheGivenOneAndNotTheChildrens()
    {
        var command = new CompositeCommand("Place a divider", [
            new AddComponent("resistor", new GridPoint(0, 0), Orientation.Default),
            new AddComponent("resistor", new GridPoint(0, 8), Orientation.Default),
        ]);

        Assert.Equal("Place a divider", command.Description);
        Assert.Equal("Place a divider", command.DescriptionOf(Schematic.Empty));
    }

    [Fact]
    public void ACompositeNeedsADescriptionAndSomeCommands()
    {
        Assert.Equal(
            "description",
            Assert.Throws<ArgumentException>(() => new CompositeCommand(string.Empty, [])).ParamName);
        Assert.Equal(
            "description",
            Assert.Throws<ArgumentNullException>(() => new CompositeCommand(null!, [])).ParamName);
        Assert.Equal(
            "commands",
            Assert.Throws<ArgumentNullException>(() => new CompositeCommand("Do something", null!)).ParamName);
    }

    [Fact]
    public void CompositesNest()
    {
        var command = new CompositeCommand("Outer", [
            new CompositeCommand("Inner", [
                new AddComponent("resistor", GridPoint.Origin, Orientation.Default),
            ]),
            new AddComponent("capacitor", new GridPoint(8, 0), Orientation.Default),
        ]);

        var schematic = command.Succeed(Schematic.Empty);

        Assert.Equal(["R1", "C1"], schematic.Components.Values.Select(component => component.Designator));
        Assert.Equal("Outer", command.DescriptionOf(Schematic.Empty));
    }
}
