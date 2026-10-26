using Godot;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Editor;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;
using Nodal.Game.Style;

namespace Nodal.Game.Presentation;

public sealed partial class PropertiesDialog : ConfirmationDialog
{
    private const string FontColorOverride = "font_color";
    private const string TitleSeparator = " — ";
    private const string NoParametersText = "No parameters";
    private const string NotSetPlaceholder = "not set";
    private const string BasicSectionTitle = "Basic";
    private const string AdvancedSectionTitle = "Advanced";
    private const string SuggestionPrefix = "Use ";
    private const string NoFunctionLabel = "None";
    private const int NoFunctionId = -1;
    private const int LabelMinimumWidth = 230;
    private const int FieldMinimumWidth = 150;
    private const int DialogMinimumWidth = 560;
    private const int DialogMinimumHeight = 160;

    private readonly VBoxContainer _body = new();
    private readonly List<ParameterRow> _rows = [];

    private ParameterForm? _form;

    public PropertiesDialog()
    {
        MinSize = new Vector2I(DialogMinimumWidth, DialogMinimumHeight);
        Confirmed += OnConfirmed;

        AddChild(_body);
    }

    public event Action<IEditCommand>? Applied;

    public void Open(ComponentInstance component, ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(definition);

        _form = ParameterForm.For(component, definition);

        Title = definition.Name + TitleSeparator + component.Designator;

        Build(definition);
        Refresh();
        PopupCentered();

        Callable.From(FocusFirstField).CallDeferred();
    }

    private void FocusFirstField()
    {
        var row = _rows.FirstOrDefault(candidate => candidate.Edit.IsVisibleInTree());

        if (row is null)
        {
            return;
        }

        row.Edit.GrabFocus();
        row.Edit.SelectAll();
    }

    private void Build(ComponentDefinition definition)
    {
        _rows.Clear();

        foreach (var child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        _body.AddChild(Description(definition.Description));

        var basic = _form!.FieldsOf(ParameterTier.Basic);
        var advanced = _form.FieldsOf(ParameterTier.Advanced);

        if (basic.Length == 0 && advanced.Length == 0)
        {
            _body.AddChild(new Label { Text = NoParametersText });

            return;
        }

        if (basic.Length > 0)
        {
            _body.AddChild(new Label { Text = BasicSectionTitle });
            _body.AddChild(Rows(basic));
        }

        if (advanced.Length > 0)
        {
            var section = new FoldableContainer { Title = AdvancedSectionTitle, Folded = true };

            section.AddChild(Rows(advanced));
            _body.AddChild(section);
        }
    }

    private static Label Description(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        CustomMinimumSize = new Vector2(DialogMinimumWidth, 0),
    };

    private VBoxContainer Rows(IEnumerable<ParameterField> fields)
    {
        var rows = new VBoxContainer();

        foreach (var field in fields)
        {
            rows.AddChild(BuildRow(field));
        }

        return rows;
    }

    private Control BuildRow(ParameterField field)
    {
        var container = new VBoxContainer();
        var line = new HBoxContainer();

        var edit = new LineEdit
        {
            Text = field.Text,
            PlaceholderText = NotSetPlaceholder,
            CustomMinimumSize = new Vector2(FieldMinimumWidth, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };

        var suggestion = new Button { Visible = false };
        var error = new Label { Visible = false };

        error.AddThemeColorOverride(FontColorOverride, VisualTokens.Error);

        var row = new ParameterRow(field, edit, error, suggestion);

        edit.TextChanged += text => OnTextChanged(row, text);
        edit.TextSubmitted += _ => OnSubmitted();
        edit.GuiInput += @event => OnFieldInput(row, @event);
        suggestion.Pressed += () => OnSuggestionPressed(row);

        line.AddChild(RowLabel(field.Definition));
        line.AddChild(edit);

        if (field.IsWaveform)
        {
            line.AddChild(TemplatePicker(row));
        }

        line.AddChild(suggestion);

        container.AddChild(line);
        container.AddChild(error);

        _rows.Add(row);

        return container;
    }

    private OptionButton TemplatePicker(ParameterRow row)
    {
        var picker = DialogStyle.TemplatePicker();

        picker.AddItem(NoFunctionLabel, NoFunctionId);

        foreach (var kind in Enum.GetValues<WaveformKind>())
        {
            picker.AddItem(NameOf(kind), (int)kind);
        }

        picker.Selected = 0;
        picker.ItemSelected += id => OnTemplateChosen(row, picker, id);

        return picker;
    }

    private static string NameOf(WaveformKind kind) =>
        WaveformText.TemplateOf(kind).Split('(')[0];

    private void OnTemplateChosen(ParameterRow row, OptionButton picker, long index)
    {
        var id = picker.GetItemId((int)index);

        row.Edit.Text = id == NoFunctionId
            ? string.Empty
            : WaveformText.TemplateOf((WaveformKind)id);

        picker.Selected = 0;

        OnTextChanged(row, row.Edit.Text);

        row.Edit.CaretColumn = row.Edit.Text.Length;
        row.Edit.GrabFocus();
    }

    private static Label RowLabel(ParameterDefinition definition) => new()
    {
        Text = LabelTextOf(definition),
        CustomMinimumSize = new Vector2(LabelMinimumWidth, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static string LabelTextOf(ParameterDefinition definition)
    {
        var symbol = definition.Unit.Symbol();

        return symbol.Length == 0 ? definition.Label : $"{definition.Label} ({symbol})";
    }

    private void OnTextChanged(ParameterRow row, string text)
    {
        _form?.Write(row.Field, text);

        Refresh();
    }

    private void OnSuggestionPressed(ParameterRow row)
    {
        _form?.ApplySuggestion(row.Field);

        Reload(row);
    }

    private void OnFieldInput(ParameterRow row, InputEvent @event)
    {
        if (_form is null || !row.Field.IsSteppable || @event is not InputEventMouseButton button
            || !button.Pressed)
        {
            return;
        }

        switch (button.ButtonIndex)
        {
            case MouseButton.WheelUp:
                _form.Step(row.Field, StepDirection.Up);
                break;

            case MouseButton.WheelDown:
                _form.Step(row.Field, StepDirection.Down);
                break;

            default:
                return;
        }

        row.Edit.AcceptEvent();
        Reload(row);
    }

    private void OnSubmitted()
    {
        if (_form is { IsValid: true })
        {
            GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
        }
    }

    private void OnConfirmed()
    {
        if (_form?.BuildCommand() is { } command)
        {
            Applied?.Invoke(command);
        }
    }

    private void Reload(ParameterRow row)
    {
        row.Edit.Text = row.Field.Text;
        row.Edit.CaretColumn = row.Field.Text.Length;

        Refresh();
    }

    private void Refresh()
    {
        foreach (var row in _rows)
        {
            row.Error.Text = row.Field.Error ?? string.Empty;
            row.Error.Visible = row.Field.Error is not null;
            row.Suggestion.Text = row.Field.Suggestion is { } suggestion
                ? SuggestionPrefix + suggestion
                : string.Empty;
            row.Suggestion.Visible = row.Field.Suggestion is not null;
        }

        GetOkButton().Disabled = _form is null || !_form.IsValid;
    }

    private sealed record ParameterRow(
        ParameterField Field, LineEdit Edit, Label Error, Button Suggestion);
}
