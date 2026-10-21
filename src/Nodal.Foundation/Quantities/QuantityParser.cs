using System.Globalization;

namespace Nodal.Foundation.Quantities;

internal static class QuantityParser
{
    private const char UppercaseResistanceDecimalMarker = 'R';
    private const char LowercaseResistanceDecimalMarker = 'r';

    private const NumberStyles NumberFormat =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

    private readonly record struct PrefixMatch(int Index, int Length, int Exponent, bool IsDecimalMarker);

    public static bool TryParse(string? text, Unit expectedUnit, out Quantity result, out QuantityParseError error)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = QuantityParseError.Empty;
            return false;
        }

        if (!TryStripUnitSymbol(text.Trim(), expectedUnit, out var withoutUnit, out error))
        {
            return false;
        }

        if (!TryRemoveSingleInnerSpace(withoutUnit, out var compact))
        {
            error = QuantityParseError.MalformedNumber;
            return false;
        }

        if (!TryReadValue(compact, expectedUnit, out var value, out error))
        {
            return false;
        }

        result = new Quantity(value, expectedUnit);
        error = QuantityParseError.None;
        return true;
    }

    private static bool TryStripUnitSymbol(
        string text, Unit expectedUnit, out string remainder, out QuantityParseError error)
    {
        remainder = text;
        error = QuantityParseError.None;

        if (!UnitSymbolSuffix.TryMatch(text, out var matchedUnit, out var matchLength))
        {
            return true;
        }

        if (matchedUnit != expectedUnit)
        {
            error = QuantityParseError.WrongUnit;
            return false;
        }

        remainder = text[..^matchLength].TrimEnd();

        if (remainder.Length == 0)
        {
            error = QuantityParseError.MalformedNumber;
            return false;
        }

        return true;
    }

    private static bool TryRemoveSingleInnerSpace(string text, out string compact)
    {
        compact = text;

        var spaceIndex = IndexOfWhiteSpace(text);
        if (spaceIndex < 0)
        {
            return true;
        }

        var head = text[..spaceIndex];
        var tail = text[(spaceIndex + 1)..];

        if (head.Length == 0 || tail.Length == 0 || IndexOfWhiteSpace(tail) >= 0)
        {
            return false;
        }

        if (char.IsAsciiDigit(tail[0]))
        {
            return false;
        }

        compact = head + tail;
        return true;
    }

    private static int IndexOfWhiteSpace(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TryReadValue(string text, Unit expectedUnit, out double value, out QuantityParseError error)
    {
        value = 0;

        if (IsNonFiniteLiteral(text))
        {
            error = QuantityParseError.OutOfRange;
            return false;
        }

        if (TryParseNumber(text, out var plain))
        {
            if (!double.IsFinite(plain))
            {
                error = QuantityParseError.OutOfRange;
                return false;
            }

            value = plain;
            error = QuantityParseError.None;
            return true;
        }

        if (!TryFindPrefix(text, expectedUnit, out var prefix))
        {
            error = ContainsLetterOutsideExponent(text)
                ? QuantityParseError.UnknownPrefix
                : QuantityParseError.MalformedNumber;
            return false;
        }

        if (!TryComposeNumberText(text, prefix, out var numberText)
            || !TryScale(numberText, prefix.Exponent, out var scaled))
        {
            error = QuantityParseError.MalformedNumber;
            return false;
        }

        if (!double.IsFinite(scaled))
        {
            error = QuantityParseError.OutOfRange;
            return false;
        }

        value = scaled;
        error = QuantityParseError.None;
        return true;
    }

    private static bool TryFindPrefix(string text, Unit expectedUnit, out PrefixMatch prefix)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (MatchesSpiceMega(text, index))
            {
                prefix = new PrefixMatch(index, MetricPrefixes.SpiceMega.Length, 6, false);
                return true;
            }

            if (MetricPrefixes.TryExponent(text[index], out var exponent))
            {
                prefix = new PrefixMatch(index, 1, exponent, false);
                return true;
            }

            if (expectedUnit == Unit.Ohm && IsResistanceDecimalMarker(text[index]))
            {
                prefix = new PrefixMatch(index, 1, 0, true);
                return true;
            }
        }

        prefix = default;
        return false;
    }

    private static bool IsResistanceDecimalMarker(char character) =>
        character is UppercaseResistanceDecimalMarker or LowercaseResistanceDecimalMarker;

    private static bool MatchesSpiceMega(string text, int index) =>
        index + MetricPrefixes.SpiceMega.Length <= text.Length
        && text.AsSpan(index, MetricPrefixes.SpiceMega.Length)
            .Equals(MetricPrefixes.SpiceMega, StringComparison.OrdinalIgnoreCase);

    private static bool TryComposeNumberText(string text, PrefixMatch prefix, out string numberText)
    {
        numberText = string.Empty;

        var head = text[..prefix.Index];
        var tail = text[(prefix.Index + prefix.Length)..];

        if (head.Length == 0)
        {
            if (!prefix.IsDecimalMarker)
            {
                return false;
            }

            head = "0";
        }

        if (tail.Length == 0)
        {
            numberText = head;
            return true;
        }

        if (!IsDigitsOnly(tail) || !IsSignedDigitsOnly(head))
        {
            return false;
        }

        numberText = head + "." + tail;
        return true;
    }

    private static bool TryScale(string numberText, int exponent, out double value)
    {
        if (exponent != 0 && !ContainsExponentMarker(numberText))
        {
            return TryParseNumber(numberText + "e" + exponent.ToString(CultureInfo.InvariantCulture), out value);
        }

        if (!TryParseNumber(numberText, out var mantissa))
        {
            value = 0;
            return false;
        }

        value = exponent == 0 ? mantissa : mantissa * Math.Pow(10, exponent);
        return true;
    }

    private static bool TryParseNumber(string text, out double value) =>
        double.TryParse(text.Replace(',', '.'), NumberFormat, CultureInfo.InvariantCulture, out value);

    private static bool IsNonFiniteLiteral(string text)
    {
        var body = text.TrimStart('+', '-');
        return body.Equals("NaN", StringComparison.OrdinalIgnoreCase)
            || body.Equals("Infinity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsExponentMarker(string text) => text.Contains('e') || text.Contains('E');

    private static bool ContainsLetterOutsideExponent(string text) =>
        text.Any(character => char.IsLetter(character) && character is not ('e' or 'E'));

    private static bool IsDigitsOnly(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);

    private static bool IsSignedDigitsOnly(string text) =>
        IsDigitsOnly(text.Length > 0 && text[0] is '+' or '-' ? text[1..] : text);
}
