namespace Nodal.Content.Components;

internal static class ComponentNameRules
{
    public const string GroundNetName = "0";

    private const char HyphenSeparator = '-';

    public static bool IsComponentId(string text) =>
        text.Length > 0 && text.All(IsIdCharacter);

    public static bool IsParameterName(string text)
    {
        if (text.Length == 0 || !char.IsAsciiLetterLower(text[0]) || text[^1] == HyphenSeparator)
        {
            return false;
        }

        for (var index = 1; index < text.Length; index++)
        {
            if (text[index] == HyphenSeparator)
            {
                if (text[index - 1] == HyphenSeparator)
                {
                    return false;
                }
            }
            else if (!IsParameterNameCharacter(text[index]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsDesignatorPrefix(string text) =>
        text.Length > 0 && text.All(char.IsAsciiLetterUpper);

    public static bool IsGlobalNetName(string text) =>
        text == GroundNetName ||
        (text.Length > 0 && char.IsAsciiLetterUpper(text[0]) && text.All(IsGlobalNetCharacter));

    private static bool IsIdCharacter(char character) =>
        char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-';

    private static bool IsGlobalNetCharacter(char character) =>
        char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character == '_';

    private static bool IsParameterNameCharacter(char character) =>
        char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character);
}
