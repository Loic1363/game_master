using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class SchematicFileRoundTripTests
{
    private const string PowerRating = "power-rating";

    [Fact]
    public void TheDemoSchematicSurvivesWritingAndReading()
    {
        var schematic = GoldenSchematic.Build();

        var result = RoundTrip(schematic);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        SchematicAssert.Equal(schematic, result.Schematic);
    }

    [Fact]
    public void AnEmptySchematicSurvivesWritingAndReading()
    {
        var result = RoundTrip(Schematic.Empty);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        SchematicAssert.Equal(Schematic.Empty, result.Schematic);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1e-12)]
    [InlineData(3333.3333)]
    [InlineData(0.1)]
    [InlineData(1234567.8912345678)]
    [InlineData(1e30)]
    public void AParameterValueSurvivesExactly(double value)
    {
        var schematic = new AddComponent("resistor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        schematic = new SetParameter(ComponentId.First, PowerRating, new Quantity(value, Unit.Watt))
            .Succeed(schematic);

        var result = RoundTrip(schematic);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.True(result.Schematic.TryGetComponent(ComponentId.First, out var component));
        Assert.Equal(value, component.Parameters[PowerRating].Quantity.Value);
        SchematicAssert.Equal(schematic, result.Schematic);
    }

    [Fact]
    public void EveryRotationAndMirrorSurvivesWritingAndReading()
    {
        var schematic = Schematic.Empty;
        var column = 0;

        foreach (var orientation in AllOrientations())
        {
            schematic = new AddComponent("resistor", new GridPoint(column, 0), orientation)
                .Succeed(schematic);

            column += 6;
        }

        var result = RoundTrip(schematic);

        Assert.True(result.Loaded);
        Assert.Equal(
            AllOrientations(),
            result.Schematic!.Components.Values.Select(component => component.Orientation));

        SchematicAssert.Equal(schematic, result.Schematic);
    }

    [Fact]
    public void NonContiguousIdentifiersSurviveWritingAndReading()
    {
        var schematic = GoldenSchematic.Build();

        schematic = new RemoveComponent(new ComponentId(2)).Succeed(schematic);
        schematic = new RemoveComponent(new ComponentId(4)).Succeed(schematic);
        schematic = new RemoveWire(new WireId(3)).Succeed(schematic);

        var result = RoundTrip(schematic);

        Assert.True(result.Loaded);
        Assert.Equal([1, 3, 5], result.Schematic.Components.Keys.Select(id => id.Value));
        Assert.Equal([1, 2, 4, 5, 6], result.Schematic.Wires.Keys.Select(id => id.Value));
        Assert.Equal(schematic.NextComponentId, result.Schematic.NextComponentId);
        Assert.Equal(schematic.NextWireId, result.Schematic.NextWireId);
        SchematicAssert.Equal(schematic, result.Schematic);
    }

    private static IEnumerable<Orientation> AllOrientations()
    {
        foreach (var rotation in Enum.GetValues<Rotation>())
        {
            yield return new Orientation(rotation, false);
            yield return new Orientation(rotation, true);
        }
    }

    private static SchematicReadResult RoundTrip(Schematic schematic) =>
        SchematicFile.Read(SchematicFile.Write(schematic), BaseCatalog.Catalog);
}
