using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Editor.Tests;

public class ParameterFormTests
{
    [Fact]
    public void ARequiredFieldStartsOnItsFormattedValue()
    {
        var field = FieldOf(Form("capacitor"), "capacitance");

        Assert.Equal("100 nF", field.Text);
        Assert.Equal(new Quantity(100e-9, Unit.Farad), field.Value);
        Assert.Null(field.Error);
        Assert.False(field.HasChanged);
    }

    [Fact]
    public void AnOptionalFieldStartsEmptyAndUnset()
    {
        var field = FieldOf(Form("capacitor"), "esr");

        Assert.Equal(string.Empty, field.Text);
        Assert.Null(field.Value);
        Assert.Null(field.Error);
        Assert.True(field.IsUnset);
        Assert.False(field.HasChanged);
    }

    [Fact]
    public void AFreshFormIsValidWithoutChangesAndBuildsNoCommand()
    {
        var form = Form("capacitor");

        Assert.True(form.IsValid);
        Assert.False(form.HasChanges);
        Assert.Null(form.BuildCommand());
    }

    [Fact]
    public void AnEmptyRequiredFieldIsRequired()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Write(field, "   ");

        Assert.Equal("Required.", field.Error);
        Assert.False(form.IsValid);
        Assert.Null(form.BuildCommand());
    }

    [Fact]
    public void AnEmptyOptionalFieldMeansUnset()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "esr");

        form.Write(field, "50m");
        form.Write(field, string.Empty);

        Assert.True(field.IsUnset);
        Assert.True(form.IsValid);
        Assert.False(form.HasChanges);
    }

    [Theory]
    [InlineData("4..7", "Not a number.")]
    [InlineData("4z7", "Unknown metric prefix.")]
    [InlineData("5 V", "Expected a value in F.")]
    [InlineData("1e400", "Out of range.")]
    public void AParseFailureGivesAReadableMessage(string text, string expected)
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Write(field, text);

        Assert.Equal(expected, field.Error);
        Assert.Null(field.Value);
        Assert.False(form.IsValid);
    }

    [Fact]
    public void AWrongUnitOnADimensionlessFieldAsksForNoUnit()
    {
        var form = ParameterForm.For(Component("widget", WidgetCatalog()), WidgetDefinition());
        var field = FieldOf(form, "gain");

        form.Write(field, "5 V");

        Assert.Equal("Expected a value with no unit.", field.Error);
    }

    [Fact]
    public void AValueBelowTheMinimumNamesTheBound()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "esr");

        form.Write(field, "-1");

        Assert.Equal($"Must be at least 0 {UnitSymbols.Ohm}.", field.Error);
        Assert.False(form.IsValid);
    }

    [Fact]
    public void AValueAboveTheMaximumNamesTheBound()
    {
        var form = ParameterForm.For(Component("widget", BoundedCatalog()), BoundedDefinition());
        var field = FieldOf(form, "gain");

        form.Write(field, "11");

        Assert.Equal("Must be at most 10.", field.Error);
    }

    [Fact]
    public void AValueOnABoundIsAccepted()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "esr");

        form.Write(field, "0");

        Assert.Null(field.Error);
        Assert.Equal(new Quantity(0, Unit.Ohm), field.Value);
    }

    [Fact]
    public void AValueOutsideTheSeriesCarriesTheNearestSuggestion()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Write(field, "5 nF");

        Assert.Equal("Not in E12.", field.Error);
        Assert.Equal(new Quantity(4.7e-9, Unit.Farad), field.Suggestion);
        Assert.Equal("4.7 nF", field.Suggestion?.ToString());
        Assert.False(form.IsValid);
    }

    [Fact]
    public void ApplyingTheSuggestionMakesTheFieldValid()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Write(field, "5 nF");
        form.ApplySuggestion(field);

        Assert.Equal("4.7 nF", field.Text);
        Assert.Null(field.Error);
        Assert.Null(field.Suggestion);
        Assert.True(form.IsValid);
        Assert.True(form.HasChanges);
    }

    [Fact]
    public void ANonPositiveValueOutsideTheSeriesCarriesNoSuggestion()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Write(field, "0");

        Assert.Equal("Not in E12.", field.Error);
        Assert.Null(field.Suggestion);
    }

    [Fact]
    public void SteppingWalksTheSeriesInBothDirections()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Step(field, StepDirection.Up);

        Assert.Equal("120 nF", field.Text);

        form.Step(field, StepDirection.Down);
        form.Step(field, StepDirection.Down);

        Assert.Equal("82 nF", field.Text);
        Assert.True(form.IsValid);
    }

    [Fact]
    public void SteppingWalksDecadesWithoutASeries()
    {
        var form = Form("voltage-source");
        var field = FieldOf(form, "voltage");

        Assert.True(field.IsSteppable);

        form.Step(field, StepDirection.Up);

        Assert.Equal("50 V", field.Text);

        form.Step(field, StepDirection.Down);
        form.Step(field, StepDirection.Down);

        Assert.Equal("500 mV", field.Text);
        Assert.True(form.IsValid);
        Assert.Equal(new Quantity(0.5, Unit.Volt), field.Value);
    }

    [Fact]
    public void SteppingDecadesLeavesABoundViolationVisible()
    {
        var form = ParameterForm.For(Component("widget", BoundedCatalog()), BoundedDefinition());
        var field = FieldOf(form, "gain");

        form.Step(field, StepDirection.Up);

        Assert.Equal("20", field.Text);
        Assert.Equal("Must be at most 10.", field.Error);
        Assert.False(form.IsValid);
    }

    [Fact]
    public void SteppingDoesNothingOnAnInvalidOrUnsetField()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "capacitance");

        form.Write(field, "oops");
        form.Step(field, StepDirection.Up);

        Assert.Equal("oops", field.Text);
        Assert.False(field.IsSteppable);

        form.Write(field, string.Empty);
        form.Step(field, StepDirection.Down);

        Assert.Equal(string.Empty, field.Text);
        Assert.False(field.IsSteppable);
    }

    [Fact]
    public void SteppingDoesNothingOnANonPositiveField()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "esr");

        form.Write(field, "0");
        form.Step(field, StepDirection.Up);

        Assert.Equal("0", field.Text);
        Assert.False(field.IsSteppable);

        form.Write(field, "-1");
        form.Step(field, StepDirection.Down);

        Assert.Equal("-1", field.Text);
        Assert.False(field.IsSteppable);
    }

    [Fact]
    public void SteppingDecadesWorksOnAnOptionalFieldOnceItHoldsAValue()
    {
        var form = Form("capacitor");
        var field = FieldOf(form, "esr");

        form.Step(field, StepDirection.Up);

        Assert.Equal(string.Empty, field.Text);

        form.Write(field, "50m");
        form.Step(field, StepDirection.Up);

        Assert.Equal("500 mΩ", field.Text);
    }

    [Fact]
    public void ChangingARequiredFieldBuildsASetCommand()
    {
        var form = Form("capacitor");

        form.Write(FieldOf(form, "capacitance"), "220 nF");

        var command = Assert.IsType<CompositeCommand>(form.BuildCommand());

        Assert.Equal("Edit C1", command.Description);

        var schematic = Apply(command, "capacitor", out var id);

        Assert.True(schematic.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(220e-9, Unit.Farad), component.Parameters["capacitance"]);
    }

    [Fact]
    public void SettingAnOptionalFieldBuildsASetCommand()
    {
        var form = Form("capacitor");

        form.Write(FieldOf(form, "voltage-rating"), "16 V");

        var schematic = Apply(form.BuildCommand(), "capacitor", out var id);

        Assert.True(schematic.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(16, Unit.Volt), component.Parameters["voltage-rating"]);
    }

    [Fact]
    public void ClearingAnOptionalFieldBuildsAClearCommand()
    {
        var history = History("capacitor", out var id);

        Assert.True(
            history.Execute(new SetParameter(id, "esr", new Quantity(0.05, Unit.Ohm))).Succeeded);
        Assert.True(history.Schematic.TryGetComponent(id, out var component));

        var form = ParameterForm.For(component, DefinitionOf("capacitor"));
        var field = FieldOf(form, "esr");

        Assert.Equal("50 m" + UnitSymbols.Ohm, field.Text);

        form.Write(field, string.Empty);

        var command = form.BuildCommand();

        Assert.NotNull(command);
        Assert.True(history.Execute(command).Succeeded);
        Assert.True(history.Schematic.TryGetComponent(id, out var edited));
        Assert.Equal(["capacitance"], edited.Parameters.Keys);
        Assert.Equal("Edit C1", history.UndoDescription);
    }

    [Fact]
    public void OneCompositeCommandCarriesEveryChangedField()
    {
        var form = Form("capacitor");

        form.Write(FieldOf(form, "capacitance"), "220 nF");
        form.Write(FieldOf(form, "esr"), "50m");

        var schematic = Apply(form.BuildCommand(), "capacitor", out var id);

        Assert.True(schematic.TryGetComponent(id, out var component));
        Assert.Equal(new Quantity(220e-9, Unit.Farad), component.Parameters["capacitance"]);
        Assert.Equal(new Quantity(0.05, Unit.Ohm), component.Parameters["esr"]);
    }

    [Fact]
    public void RewritingTheSameValueCountsAsNoChange()
    {
        var form = Form("capacitor");

        form.Write(FieldOf(form, "capacitance"), "0.1 uF");

        Assert.True(form.IsValid);
        Assert.False(form.HasChanges);
        Assert.Null(form.BuildCommand());
    }

    [Fact]
    public void AnInvalidFormBuildsNoCommand()
    {
        var form = Form("capacitor");

        form.Write(FieldOf(form, "esr"), "50m");
        form.Write(FieldOf(form, "capacitance"), string.Empty);

        Assert.True(form.HasChanges);
        Assert.Null(form.BuildCommand());
    }

    [Fact]
    public void FieldsAreSplitByTier()
    {
        var form = Form("capacitor");

        Assert.Equal(
            ["capacitance"],
            form.FieldsOf(ParameterTier.Basic).Select(field => field.Definition.Name));

        Assert.Equal(6, form.FieldsOf(ParameterTier.Advanced).Length);
        Assert.Equal(7, form.Fields.Length);
    }

    [Fact]
    public void AComponentWithoutParametersHasNoFields()
    {
        var form = Form("npn");

        Assert.Empty(form.Fields);
        Assert.True(form.IsValid);
        Assert.Null(form.BuildCommand());
        Assert.Equal("Q1", form.Designator);
    }

    [Fact]
    public void AFieldOfAnotherFormIsRejected()
    {
        var form = Form("capacitor");
        var other = Form("capacitor");

        Assert.Throws<ArgumentException>(() => form.Write(FieldOf(other, "esr"), "1"));
    }

    private static ParameterForm Form(string definitionId) =>
        ParameterForm.For(Component(definitionId, BaseContent.Catalog), DefinitionOf(definitionId));

    private static ParameterField FieldOf(ParameterForm form, string parameterName) =>
        Assert.Single(form.Fields, field => field.Definition.Name == parameterName);

    private static ComponentDefinition DefinitionOf(string definitionId)
    {
        Assert.True(BaseContent.Catalog.TryGetById(definitionId, out var definition));

        return definition;
    }

    private static ComponentInstance Component(string definitionId, ComponentCatalog catalog)
    {
        var result = new AddComponent(definitionId, new GridPoint(1, 2), Orientation.Default)
            .Apply(Schematic.Empty, catalog);

        Assert.True(result.Succeeded);

        return Assert.Single(result.Schematic!.Components.Values);
    }

    private static EditHistory History(string definitionId, out ComponentId id)
    {
        var history = new EditHistory(Schematic.Empty, BaseContent.Catalog);

        id = Schematic.Empty.NextComponentId;

        Assert.True(history
            .Execute(new AddComponent(definitionId, new GridPoint(1, 2), Orientation.Default))
            .Succeeded);

        return history;
    }

    private static Schematic Apply(IEditCommand? command, string definitionId, out ComponentId id)
    {
        var history = History(definitionId, out id);

        Assert.NotNull(command);
        Assert.True(history.Execute(command).Succeeded);

        return history.Schematic;
    }

    private static ComponentCatalog WidgetCatalog() => new([WidgetDefinition()]);

    private static ComponentCatalog BoundedCatalog() => new([BoundedDefinition()]);

    private static ComponentDefinition WidgetDefinition() => Widget(null, null);

    private static ComponentDefinition BoundedDefinition() => Widget(
        new Quantity(0, Unit.Dimensionless), new Quantity(10, Unit.Dimensionless));

    private static ComponentDefinition Widget(Quantity? minimum, Quantity? maximum) => new(
        "widget",
        "Widget",
        "A widget.",
        ComponentCategory.Passive,
        "W",
        [new PinDefinition("1", -1, 0)],
        [
            new ParameterDefinition(
                "gain",
                "Gain",
                ParameterTier.Basic,
                Unit.Dimensionless,
                new Quantity(2, Unit.Dimensionless),
                minimum,
                maximum,
                null),
        ]);
}
