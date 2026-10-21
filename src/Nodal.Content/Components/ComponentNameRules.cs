namespace Nodal.Content.Components;

internal static class ComponentNameRules
{
    public static bool IsComponentId(string text) =>
        text.Length > 0 && text.All(IsIdCharacter);

    public static bool IsParameterName(string text) =>
        text.Length > 0 && char.IsAsciiLetterLower(text[0]) && text.All(IsParameterNameCharacter);

    public static bool IsDesignatorPrefix(string text) =>
        text.Length > 0 && text.All(char.IsAsciiLetterUpper);

    private static bool IsIdCharacter(char character) =>
        char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-';

    private static bool IsParameterNameCharacter(char character) =>
        char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character);
}
