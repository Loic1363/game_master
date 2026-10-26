using Godot;

namespace Nodal.Game.Style;

public static class InlineEditStyle
{
    public const float MinimumWidth = 96f;
    public const float PaddingX = 6f;
    public const float PaddingY = 2f;

    private const string FontColorOverride = "font_color";
    private const string FontSizeOverride = "font_size";
    private const float BorderWidth = 1f;
    private const float CornerRadius = 3f;

    public static LineEdit Create()
    {
        var edit = new LineEdit
        {
            Alignment = HorizontalAlignment.Center,
            SelectAllOnFocus = true,
        };

        edit.AddThemeColorOverride(FontColorOverride, VisualTokens.Stroke);
        edit.AddThemeStyleboxOverride("normal", Box(VisualTokens.Selection));
        edit.AddThemeStyleboxOverride("focus", Box(VisualTokens.Selection));

        return edit;
    }

    public static void Resize(LineEdit edit, Rect2 label, int fontSize)
    {
        ArgumentNullException.ThrowIfNull(edit);

        var width = Math.Max(MinimumWidth, label.Size.X + (2 * PaddingX));
        var height = label.Size.Y + (2 * PaddingY);
        var center = label.Position + (label.Size / 2f);

        edit.AddThemeFontSizeOverride(FontSizeOverride, fontSize);
        edit.Size = new Vector2(width, height);
        edit.Position = center - (edit.Size / 2f);
    }

    private static StyleBoxFlat Box(Color border) => new()
    {
        BgColor = VisualTokens.LabelPill,
        BorderColor = border,
        BorderWidthLeft = (int)BorderWidth,
        BorderWidthRight = (int)BorderWidth,
        BorderWidthTop = (int)BorderWidth,
        BorderWidthBottom = (int)BorderWidth,
        CornerRadiusTopLeft = (int)CornerRadius,
        CornerRadiusTopRight = (int)CornerRadius,
        CornerRadiusBottomLeft = (int)CornerRadius,
        CornerRadiusBottomRight = (int)CornerRadius,
        ContentMarginLeft = PaddingX,
        ContentMarginRight = PaddingX,
        ContentMarginTop = PaddingY,
        ContentMarginBottom = PaddingY,
    };
}
