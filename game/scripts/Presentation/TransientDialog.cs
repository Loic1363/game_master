using Godot;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Nodal.Game.Style;

namespace Nodal.Game.Presentation;

public sealed partial class TransientDialog : ConfirmationDialog
{
    private const string DialogTitle = "Transient analysis";
    private const string RunLabel = "Run";
    private const string OptionalPlaceholder = "automatic";
    private const string RequiredPlaceholder = "required";
    private const string Explanation =
        "The analysis runs from zero to the stop time. Leave the steps empty to let the "
        + "simulator choose them.";

    private readonly VBoxContainer _body = new();
    private readonly List<FieldRow> _rows = [];

    private CheckBox? _startFromZero;
    private TransientForm? _form;

    public TransientDialog()
    {
        Title = DialogTitle;
        OkButtonText = RunLabel;

        DialogStyle.ApplyMinimumSize(this);

        Confirmed += OnConfirmed;

        AddChild(_body);
    }

    public event Action<TransientSettings, IEditCommand?>? Requested;

    public void Open(AnalysisSettings analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        _form = TransientForm.For(analysis);

        Build();
        Refresh();
        PopupCentered();

        Callable.From(FocusFirstField).CallDeferred();
    }

    private void Build()
    {
        _rows.Clear();

        foreach (var child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        _body.AddChild(DialogStyle.WrappedLabel(Explanation));

        foreach (var field in _form!.Fields)
        {
            _body.AddChild(BuildRow(field));
        }

        _startFromZero = DialogStyle.Checkbox(TransientForm.StartFromZeroLabel);
        _startFromZero.ButtonPressed = _form.StartFromZero;
        _startFromZero.Toggled += OnStartFromZeroToggled;

        _body.AddChild(_startFromZero);
    }

    private Control BuildRow(TimeField field)
    {
        var container = new VBoxContainer();
        var line = new HBoxContainer();
        var edit = DialogStyle.Field(field.IsRequired ? RequiredPlaceholder : OptionalPlaceholder);
        var error = DialogStyle.ErrorLabel();

        edit.Text = field.Text;

        var row = new FieldRow(field, edit, error);

        edit.TextChanged += text => OnTextChanged(row, text);
        edit.TextSubmitted += _ => OnSubmitted();

        line.AddChild(DialogStyle.RowLabel(LabelOf(field)));
        line.AddChild(edit);

        container.AddChild(line);
        container.AddChild(error);

        _rows.Add(row);

        return container;
    }

    private static string LabelOf(TimeField field) => $"{field.Label} (s)";

    private void FocusFirstField()
    {
        var row = _rows.FirstOrDefault();

        row?.Edit.GrabFocus();
        row?.Edit.SelectAll();
    }

    private void OnTextChanged(FieldRow row, string text)
    {
        _form?.Write(row.Field, text);

        Refresh();
    }

    private void OnStartFromZeroToggled(bool pressed)
    {
        if (_form is not null)
        {
            _form.StartFromZero = pressed;
        }
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
        if (_form is { IsValid: true, Settings: { } settings })
        {
            Requested?.Invoke(settings, _form.BuildCommand());
        }
    }

    private void Refresh()
    {
        foreach (var row in _rows)
        {
            row.Error.Text = row.Field.Error ?? string.Empty;
            row.Error.Visible = row.Field.Error is not null;
        }

        GetOkButton().Disabled = _form is null || !_form.IsValid;
    }

    private sealed record FieldRow(TimeField Field, LineEdit Edit, Label Error);
}
