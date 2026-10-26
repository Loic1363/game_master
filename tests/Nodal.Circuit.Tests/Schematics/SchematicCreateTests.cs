using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Schematics;

public class SchematicCreateTests
{
    [Fact]
    public void CreateHoldsTheGivenComponentsWiresAndNextIdentifiers()
    {
        var schematic = Schematic.Create(
            [Resistor(1, "R1", 0), Resistor(4, "R2", 6)],
            [Segment(2), Segment(7)],
            new ComponentId(5),
            new WireId(8));

        Assert.Equal([1, 4], schematic.Components.Keys.Select(id => id.Value));
        Assert.Equal([2, 7], schematic.Wires.Keys.Select(id => id.Value));
        Assert.Equal(5, schematic.NextComponentId.Value);
        Assert.Equal(8, schematic.NextWireId.Value);
    }

    [Fact]
    public void CreateAcceptsAnEmptySchematic()
    {
        var schematic = Schematic.Create([], [], ComponentId.First, WireId.First);

        Assert.Empty(schematic.Components);
        Assert.Empty(schematic.Wires);
    }

    [Fact]
    public void CreateRejectsADuplicateComponentId()
    {
        var failure = Assert.Throws<ArgumentException>(() => Schematic.Create(
            [Resistor(1, "R1", 0), Resistor(1, "R2", 6)], [], new ComponentId(2), WireId.First));

        Assert.Contains("The component id 1 is used more than once.", failure.Message);
    }

    [Fact]
    public void CreateRejectsADuplicateDesignator()
    {
        var failure = Assert.Throws<ArgumentException>(() => Schematic.Create(
            [Resistor(1, "R1", 0), Resistor(2, "R1", 6)], [], new ComponentId(3), WireId.First));

        Assert.Contains("The designator 'R1' is used more than once.", failure.Message);
    }

    [Fact]
    public void CreateRejectsADuplicateWireId()
    {
        var failure = Assert.Throws<ArgumentException>(() => Schematic.Create(
            [], [Segment(1), Segment(1)], ComponentId.First, new WireId(2)));

        Assert.Contains("The wire id 1 is used more than once.", failure.Message);
    }

    [Fact]
    public void CreateRejectsANextComponentIdThatIsNotGreaterThanEveryUsedId()
    {
        var failure = Assert.Throws<ArgumentException>(() => Schematic.Create(
            [Resistor(1, "R1", 0), Resistor(7, "R2", 6)], [], new ComponentId(7), WireId.First));

        Assert.Contains("must be greater than the used id 7", failure.Message);
    }

    [Fact]
    public void CreateRejectsANextWireIdThatIsNotGreaterThanEveryUsedId()
    {
        var failure = Assert.Throws<ArgumentException>(() => Schematic.Create(
            [], [Segment(3)], ComponentId.First, new WireId(3)));

        Assert.Contains("must be greater than the used id 3", failure.Message);
    }

    [Fact]
    public void CreateRejectsNextIdentifiersBelowTheFirstOne()
    {
        Assert.Throws<ArgumentException>(() => Schematic.Create(
            [], [], new ComponentId(0), WireId.First));

        Assert.Throws<ArgumentException>(() => Schematic.Create(
            [], [], ComponentId.First, new WireId(0)));
    }

    [Fact]
    public void CreateRejectsMissingCollections()
    {
        Assert.Throws<ArgumentNullException>(() => Schematic.Create(
            null!, [], ComponentId.First, WireId.First));

        Assert.Throws<ArgumentNullException>(() => Schematic.Create(
            [], null!, ComponentId.First, WireId.First));
    }

    private static ComponentInstance Resistor(int id, string designator, int x) => new(
        new ComponentId(id),
        "resistor",
        designator,
        new GridPoint(x, 0),
        Orientation.Default,
        ImmutableSortedDictionary.Create<string, ParameterValue>(StringComparer.Ordinal)
            .Add("resistance", new Quantity(1000, Unit.Ohm)));

    private static Wire Segment(int id) =>
        new(new WireId(id), new GridPoint(0, id), new GridPoint(4, id));
}
