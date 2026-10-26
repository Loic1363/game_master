using Godot;

namespace Nodal.Game.Style;

public static class DialogStyle
{
    public const int LabelWidth = 230;
    public const int FieldWidth = 150;
    public const int MinimumWidth = 560;
    public const int MinimumHeight = 160;
    public const int TemplateWidth = 120;

    private const string FontColorOverride = "font_color";

    public static void ApplyMinimumSize(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        dialog.MinSize = new Vector2I(MinimumWidth, MinimumHeight);
    }

    public static Label SectionLabel(string text) => new() { Text = text };

    public static Label RowLabel(string text) => new()
    {
        Text = text,
        CustomMinimumSize = new Vector2(LabelWidth, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    public static Label ErrorLabel()
    {
        var label = new Label { Visible = false };

        label.AddThemeColorOverride(FontColorOverride, VisualTokens.Error);

        return label;
    }

    public static LineEdit Field(string placeholder) => new()
    {
        PlaceholderText = placeholder,
        CustomMinimumSize = new Vector2(FieldWidth, 0),
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    };

    public static OptionButton TemplatePicker() => new()
    {
        CustomMinimumSize = new Vector2(TemplateWidth, 0),
    };

    public static CheckBox Checkbox(string text) => new() { Text = text };

    public static Label WrappedLabel(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        CustomMinimumSize = new Vector2(MinimumWidth, 0),
    };
}
