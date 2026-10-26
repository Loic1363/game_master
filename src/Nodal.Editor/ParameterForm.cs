using System.Collections.Immutable;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Editor;

public sealed class ParameterForm
{
    private readonly ComponentId _id;

    private ParameterForm(ComponentInstance component, ComponentDefinition definition)
    {
        _id = component.Id;
        Designator = component.Designator;
        Definition = definition;
        Fields =
        [
            .. definition.Parameters.Select(
                parameter => new ParameterField(parameter, CurrentValue(component, parameter))),
        ];
    }

    public string Designator { get; }

    public ComponentDefinition Definition { get; }

    public ImmutableArray<ParameterField> Fields { get; }

    public bool IsValid => Fields.All(entry => entry.IsValid);

    public bool HasChanges => Fields.Any(entry => entry.HasChanged);

    public static ParameterForm For(ComponentInstance component, ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(definition);

        return new ParameterForm(component, definition);
    }

    public ImmutableArray<ParameterField> FieldsOf(ParameterTier tier) =>
        [.. Fields.Where(entry => entry.Definition.Tier == tier)];

    public void Write(ParameterField field, string text) => Owned(field).Write(text);

    public void ApplySuggestion(ParameterField field) => Owned(field).ApplySuggestion();

    public void Step(ParameterField field, StepDirection direction) => Owned(field).Step(direction);

    public IEditCommand? BuildCommand()
    {
        if (!IsValid)
        {
            return null;
        }

        var commands = Fields.Where(entry => entry.HasChanged).Select(CommandFor).ToArray();

        return commands.Length == 0 ? null : new CompositeCommand($"Edit {Designator}", commands);
    }

    private IEditCommand CommandFor(ParameterField field) =>
        field.Value is { } value
            ? new SetParameter(_id, field.Definition.Name, value)
            : new ClearParameter(_id, field.Definition.Name);

    private ParameterField Owned(ParameterField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return Fields.Contains(field)
            ? field
            : throw new ArgumentException("This field belongs to another form.", nameof(field));
    }

    private static ParameterValue? CurrentValue(
        ComponentInstance component, ParameterDefinition parameter) =>
        component.Parameters.TryGetValue(parameter.Name, out var value) ? value : null;
}
