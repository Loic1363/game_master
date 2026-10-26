using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Editor;

public sealed class InlineValueEditor
{
    private readonly ComponentId _id;
    private readonly ParameterField _field;

    private InlineValueEditor(ComponentInstance component, ParameterDefinition parameter)
    {
        _id = component.Id;
        _field = new ParameterField(parameter, ValueOf(component, parameter));
        Designator = component.Designator;
    }

    public string Designator { get; }

    public ParameterDefinition Parameter => _field.Definition;

    public string Text => _field.Text;

    public string? Error => _field.Error;

    public bool IsValid => _field.IsValid;

    public bool HasChanges => _field.HasChanged;

    public static bool TryOpen(
        ComponentInstance component,
        ComponentDefinition definition,
        [NotNullWhen(true)] out InlineValueEditor? editor)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(definition);

        if (EditedParameter(component, definition) is not { } parameter)
        {
            editor = null;

            return false;
        }

        editor = new InlineValueEditor(component, parameter);

        return true;
    }

    public static string? LabelTextOf(
        ComponentInstance component, ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(definition);

        if (WaveformOf(component, definition) is { } function)
        {
            return function;
        }

        if (RequiredParameter(definition) is not { } required)
        {
            return null;
        }

        return component.Parameters.TryGetValue(required.Name, out var value)
            ? value.ToString()
            : null;
    }

    public void Write(string? text) => _field.Write(text ?? string.Empty);

    public IEditCommand? BuildCommand()
    {
        if (!IsValid || !HasChanges)
        {
            return null;
        }

        return _field.Value is { } value
            ? new SetParameter(_id, Parameter.Name, value)
            : new ClearParameter(_id, Parameter.Name);
    }

    private static ParameterDefinition? EditedParameter(
        ComponentInstance component, ComponentDefinition definition)
    {
        var waveform = WaveformParameter(definition);

        if (waveform is not null
            && component.Parameters.TryGetValue(waveform.Name, out var value)
            && value.IsWaveform)
        {
            return waveform;
        }

        return RequiredParameter(definition) ?? waveform;
    }

    private static ParameterDefinition? WaveformParameter(ComponentDefinition definition) =>
        definition.Parameters.FirstOrDefault(parameter => parameter.IsWaveform);

    private static ParameterDefinition? RequiredParameter(ComponentDefinition definition) =>
        definition.Parameters.FirstOrDefault(parameter => parameter.IsRequired);

    private static string? WaveformOf(
        ComponentInstance component, ComponentDefinition definition)
    {
        if (WaveformParameter(definition) is not { } parameter
            || !component.Parameters.TryGetValue(parameter.Name, out var value)
            || !value.TryGetWaveform(out var waveform))
        {
            return null;
        }

        return waveform.ToString();
    }

    private static ParameterValue? ValueOf(
        ComponentInstance component, ParameterDefinition parameter) =>
        component.Parameters.TryGetValue(parameter.Name, out var value) ? value : null;
}
