using Godot;

namespace Nodal.Game.Style;

public static class StatusBarStyle
{
    private const string FontColorOverride = "font_color";

    public static void Apply(ColorRect bar, Label label)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentNullException.ThrowIfNull(label);

        bar.Color = VisualTokens.Background;
        label.AddThemeColorOverride(FontColorOverride, VisualTokens.LabelText);
    }
}
