using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;
using Xunit;

namespace Nodal.Editor.Tests;

public class InlineValueEditorTests
{
    private const string Voltage = "voltage";
    private const string Function = "function";
    private const string Resistance = "resistance";

    private static Schematic Circuit() => Schematic.Empty
        .Add("resistor", 0, 0)
        .Add("voltage-source", 8, 0)
        .Add("ground", 16, 0);

    private static (ComponentInstance Component, ComponentDefinition Definition) Pick(
        Schematic schematic, string designator)
    {
        var component = schematic.Components.Values.First(
            candidate => string.Equals(
                candidate.Designator, designator, StringComparison.Ordinal));

        Assert.True(BaseContent.Catalog.TryGetById(component.DefinitionId, out var definition));

        return (component, definition);
    }

    private static InlineValueEditor Open(Schematic schematic, string designator)
    {
        var (component, definition) = Pick(schematic, designator);

        Assert.True(InlineValueEditor.TryOpen(component, definition, out var editor));

        return editor;
    }

    private static string? LabelOf(Schematic schematic, string designator)
    {
        var (component, definition) = Pick(schematic, designator);

        return InlineValueEditor.LabelTextOf(component, definition);
    }

    private static Schematic Apply(Schematic schematic, IEditCommand command)
    {
        var result = command.Apply(schematic, BaseContent.Catalog);

        Assert.True(result.Succeeded);

        return result.Schematic;
    }

    private static Schematic WithFunction(Schematic schematic, Waveform waveform)
    {
        var (component, _) = Pick(schematic, "V1");

        return Apply(
            schematic,
            new SetParameter(
                component.Id, Function, ParameterValue.Of(waveform, Unit.Volt)));
    }

    [Fact]
    public void TheLabelShowsTheFirstRequiredValueWithoutAFunction()
    {
        var schematic = Circuit();

        Assert.Equal("1 kΩ", LabelOf(schematic, "R1"));
        Assert.Equal("5 V", LabelOf(schematic, "V1"));
    }

    [Fact]
    public void TheLabelShowsTheFunctionTextWhenOneIsSet()
    {
        var pulse = new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 0.02, 0.04);
        var schematic = WithFunction(Circuit(), pulse);

        Assert.Equal("PULSE(0 12 0 1n 1n 20m 40m)", LabelOf(schematic, "V1"));
    }

    [Fact]
    public void AComponentWithoutParametersHasNoLabelAndCannotBeEdited()
    {
        var schematic = Circuit();
        var (component, definition) = Pick(schematic, "GND1");

        Assert.Null(InlineValueEditor.LabelTextOf(component, definition));
        Assert.False(InlineValueEditor.TryOpen(component, definition, out _));
    }

    [Fact]
    public void EditingAResistorStartsFromItsValue()
    {
        var editor = Open(Circuit(), "R1");

        Assert.Equal("1 kΩ", editor.Text);
        Assert.Equal(Resistance, editor.Parameter.Name);
        Assert.Equal("R1", editor.Designator);
        Assert.True(editor.IsValid);
        Assert.False(editor.HasChanges);
        Assert.Null(editor.BuildCommand());
    }

    [Fact]
    public void EditingASourceWithoutAFunctionStartsFromItsVoltage()
    {
        var editor = Open(Circuit(), "V1");

        Assert.Equal(Voltage, editor.Parameter.Name);
        Assert.Equal("5 V", editor.Text);
    }

    [Fact]
    public void EditingASourceWithAFunctionStartsFromTheFunctionText()
    {
        var schematic = WithFunction(Circuit(), new SineWaveform(0, 10, 50));
        var editor = Open(schematic, "V1");

        Assert.Equal(Function, editor.Parameter.Name);
        Assert.Equal("SINE(0 10 50)", editor.Text);
    }

    [Fact]
    public void ANewValueBecomesASetParameterCommand()
    {
        var schematic = Circuit();
        var editor = Open(schematic, "R1");

        editor.Write("4k7");

        Assert.True(editor.IsValid);
        Assert.True(editor.HasChanges);

        var command = Assert.IsType<SetParameter>(editor.BuildCommand());
        var updated = Apply(schematic, command);

        Assert.Equal("4.7 kΩ", LabelOf(updated, "R1"));
    }

    [Fact]
    public void ANewFunctionBecomesASetParameterCommand()
    {
        var schematic = WithFunction(Circuit(), new SineWaveform(0, 10, 50));
        var editor = Open(schematic, "V1");

        editor.Write("PULSE(0 5 0 1u 1u 0.5m 1m)");

        var command = Assert.IsType<SetParameter>(editor.BuildCommand());
        var updated = Apply(schematic, command);

        Assert.Equal("PULSE(0 5 0 1µ 1µ 500µ 1m)", LabelOf(updated, "V1"));
    }

    [Fact]
    public void ClearingAFunctionBecomesAClearParameterCommand()
    {
        var schematic = WithFunction(Circuit(), new SineWaveform(0, 10, 50));
        var editor = Open(schematic, "V1");

        editor.Write(string.Empty);

        Assert.True(editor.IsValid);

        var command = Assert.IsType<ClearParameter>(editor.BuildCommand());
        var updated = Apply(schematic, command);

        Assert.Equal("5 V", LabelOf(updated, "V1"));
    }

    [Fact]
    public void ClearingARequiredValueIsRefused()
    {
        var editor = Open(Circuit(), "R1");

        editor.Write(string.Empty);

        Assert.False(editor.IsValid);
        Assert.Equal("Required.", editor.Error);
        Assert.Null(editor.BuildCommand());
    }

    [Theory]
    [InlineData("nonsense", "Not a number.")]
    [InlineData("1 V", "Expected a value in Ω.")]
    [InlineData("10X", "Unknown metric prefix.")]
    public void AnInvalidValueIsReportedAndAppliesNothing(string text, string expected)
    {
        var editor = Open(Circuit(), "R1");

        editor.Write(text);

        Assert.False(editor.IsValid);
        Assert.Equal(expected, editor.Error);
        Assert.Null(editor.BuildCommand());
    }

    [Fact]
    public void AnInvalidFunctionIsReportedAndAppliesNothing()
    {
        var schematic = WithFunction(Circuit(), new SineWaveform(0, 10, 50));
        var editor = Open(schematic, "V1");

        editor.Write("PULSE(0 5)");

        Assert.False(editor.IsValid);
        Assert.NotNull(editor.Error);
        Assert.Null(editor.BuildCommand());
    }

    [Fact]
    public void WritingTheSameTextBackAppliesNothing()
    {
        var editor = Open(Circuit(), "R1");

        editor.Write("1k");

        Assert.True(editor.IsValid);
        Assert.False(editor.HasChanges);
        Assert.Null(editor.BuildCommand());
    }
}
