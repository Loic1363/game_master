using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class SetParameterTests
{
    [Fact]
    public void SettingAValueInTheSeriesReplacesTheDefault()
    {
        var schematic = Resistor(out var id);

        var updated = new SetParameter(id, "resistance", new Quantity(4700, Unit.Ohm)).Succeed(schematic);

        Assert.True(updated.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(4700, Unit.Ohm), component.Parameters["resistance"]);
    }

    [Fact]
    public void SettingTheValueItAlreadyHasReturnsTheSameSchematic()
    {
        var schematic = Resistor(out var id);

        var updated = new SetParameter(id, "resistance", new Quantity(1000, Unit.Ohm))
            .Succeed(schematic);

        Assert.Same(schematic, updated);
    }

    [Fact]
    public void SettingTheValueItAlreadyHasStillDescribesTheEdit()
    {
        var schematic = Resistor(out var id);
        var command = new SetParameter(id, "resistance", new Quantity(1000, Unit.Ohm));

        Assert.Equal($"Set R1 resistance to 1 k{UnitSymbols.Ohm}", command.DescriptionOf(schematic));
    }

    [Fact]
    public void AnInvalidUnitStillFailsRatherThanCountingAsANoOp()
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "resistance", new Quantity(1000, Unit.Farad))
            .Reject(schematic);

        Assert.Equal(EditErrorCode.UnitMismatch, error.Code);
    }

    [Fact]
    public void AValueBelowTheMinimumIsRejected()
    {
        var schematic = Capacitor(out var id);

        var error = new SetParameter(id, "esr", new Quantity(-1, Unit.Ohm)).Reject(schematic);

        Assert.Equal(EditErrorCode.ValueOutOfBounds, error.Code);
        Assert.Contains("below the minimum", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AValueAboveTheMaximumIsRejected()
    {
        var id = Schematic.Empty.NextComponentId;
        var catalog = BoundedCatalog();
        var schematic = new AddComponent("widget", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty, catalog);

        var error = new SetParameter(id, "gain", new Quantity(11, Unit.Dimensionless))
            .Reject(schematic, catalog);

        Assert.Equal(EditErrorCode.ValueOutOfBounds, error.Code);
        Assert.Contains("above the maximum", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AValueOnABoundIsAccepted()
    {
        var schematic = Capacitor(out var id);

        var updated = new SetParameter(id, "esr", new Quantity(0, Unit.Ohm)).Succeed(schematic);

        Assert.True(updated.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(0, Unit.Ohm), component.Parameters["esr"]);
    }

    [Fact]
    public void SettingAnOptionalParameterAddsItToTheComponent()
    {
        var schematic = Capacitor(out var id);

        var updated = new SetParameter(id, "voltage-rating", new Quantity(16, Unit.Volt))
            .Succeed(schematic);

        Assert.True(updated.TryGetComponent(id, out var component));
        Assert.Equal(["capacitance", "voltage-rating"], component.Parameters.Keys);
    }

    [Fact]
    public void SettingAValueLeavesTheRestOfTheComponentAlone()
    {
        var schematic = Resistor(out var id);

        var updated = new SetParameter(id, "resistance", new Quantity(4700, Unit.Ohm)).Succeed(schematic);

        Assert.True(updated.TryGetComponent(id, out var component));
        Assert.Equal("R1", component.Designator);
        Assert.Equal(new GridPoint(1, 2), component.Position);
        Assert.Single(component.Parameters);
    }

    [Fact]
    public void AnUnconstrainedParameterAcceptsAnyValue()
    {
        var id = Schematic.Empty.NextComponentId;
        var schematic = new AddComponent("voltage-source", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        var updated = new SetParameter(id, "voltage", new Quantity(3.3, Unit.Volt)).Succeed(schematic);

        Assert.True(updated.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(3.3, Unit.Volt), component.Parameters["voltage"]);
    }

    [Fact]
    public void SettingAParameterOnAnUnknownComponentFails()
    {
        var error = new SetParameter(new ComponentId(3), "resistance", new Quantity(1, Unit.Ohm))
            .Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.UnknownComponent, error.Code);
    }

    [Fact]
    public void SettingAParameterOfAnUnknownDefinitionFails()
    {
        var id = Schematic.Empty.NextComponentId;
        var schematic = new AddComponent("widget", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty, WidgetCatalog());

        var error = new SetParameter(id, "gain", new Quantity(2, Unit.Dimensionless))
            .Reject(schematic, ComponentCatalog.Empty);

        Assert.Equal(EditErrorCode.UnknownDefinition, error.Code);
    }

    [Fact]
    public void SettingAParameterThatTheDefinitionDoesNotDeclareFails()
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "capacitance", new Quantity(1, Unit.Farad)).Reject(schematic);

        Assert.Equal(EditErrorCode.UnknownParameter, error.Code);
        Assert.Contains("capacitance", error.Message, StringComparison.Ordinal);
        Assert.Contains("R1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ParameterNamesAreCaseSensitive()
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "Resistance", new Quantity(4700, Unit.Ohm)).Reject(schematic);

        Assert.Equal(EditErrorCode.UnknownParameter, error.Code);
    }

    [Fact]
    public void SettingAValueInTheWrongUnitFails()
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "resistance", new Quantity(4700, Unit.Farad)).Reject(schematic);

        Assert.Equal(EditErrorCode.UnitMismatch, error.Code);
    }

    [Fact]
    public void ADimensionlessValueDoesNotPassForAUnit()
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "resistance", new Quantity(4700, Unit.Dimensionless))
            .Reject(schematic);

        Assert.Equal(EditErrorCode.UnitMismatch, error.Code);
    }

    [Theory]
    [InlineData(4800)]
    [InlineData(1050)]
    [InlineData(0.5)]
    public void SettingAValueOutsideTheSeriesFails(double value)
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "resistance", new Quantity(value, Unit.Ohm)).Reject(schematic);

        Assert.Equal(EditErrorCode.ValueNotInSeries, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4700)]
    public void SettingANonPositiveConstrainedValueFails(double value)
    {
        var schematic = Resistor(out var id);

        var error = new SetParameter(id, "resistance", new Quantity(value, Unit.Ohm)).Reject(schematic);

        Assert.Equal(EditErrorCode.ValueNotInSeries, error.Code);
    }

    [Fact]
    public void TheDescriptionNamesTheDesignatorTheParameterAndTheValue()
    {
        var schematic = Resistor(out var id);
        var command = new SetParameter(id, "resistance", new Quantity(4700, Unit.Ohm));

        Assert.Equal(
            $"Set R1 resistance to 4.7 k{UnitSymbols.Ohm}", command.DescriptionOf(schematic));
    }

    private static Schematic Resistor(out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("resistor", new GridPoint(1, 2), Orientation.Default)
            .Succeed(Schematic.Empty);
    }

    private static Schematic Capacitor(out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("capacitor", new GridPoint(1, 2), Orientation.Default)
            .Succeed(Schematic.Empty);
    }

    private static ComponentCatalog WidgetCatalog() => WidgetCatalog(null, null);

    private static ComponentCatalog BoundedCatalog() => WidgetCatalog(
        new Quantity(0, Unit.Dimensionless), new Quantity(10, Unit.Dimensionless));

    private static ComponentCatalog WidgetCatalog(Quantity? minimum, Quantity? maximum) => new([
        new ComponentDefinition(
            "widget",
            "Widget",
            "A widget.",
            ComponentCategory.Passive,
            "W",
            [new PinDefinition("1", -1, 0)],
            [new ParameterDefinition(
                "gain",
                "Gain",
                ParameterTier.Basic,
                Unit.Dimensionless,
                new Quantity(2, Unit.Dimensionless),
                minimum,
                maximum,
                null)]),
    ]);
}
