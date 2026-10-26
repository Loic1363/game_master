using System.Globalization;

namespace Nodal.Foundation.Quantities;

internal static class PrefixedNumber
{
    public const int MaximumSignificantDigits = 6;

    private const string Zero = "0";
    private const string MantissaOverflowText = "1000";
    private const int MaximumDecimals = 15;

    public static string Format(double value)
    {
        if (value == 0)
        {
            return Zero;
        }

        if (!double.IsFinite(value))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        for (var digits = 1; digits < MaximumSignificantDigits; digits++)
        {
            var candidate = Candidate(value, digits);

            if (QuantityParser.TryParse(candidate, Unit.Dimensionless, out var parsed, out _)
                && parsed.Value.Equals(value))
            {
                return candidate;
            }
        }

        return Candidate(value, MaximumSignificantDigits);
    }

    private static string Candidate(double value, int digits)
    {
        var exponent = EngineeringExponent(value);
        var mantissa = FormatMantissa(Rescale(value, -exponent), digits);

        if (mantissa.TrimStart('-') == MantissaOverflowText
            && exponent < MetricPrefixes.LargestExponent)
        {
            exponent += MetricPrefixes.ExponentStep;
            mantissa = FormatMantissa(Rescale(value, -exponent), digits);
        }

        return mantissa + MetricPrefixes.SymbolFor(exponent);
    }

    private static int EngineeringExponent(double value)
    {
        var decade = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        var exponent = (int)Math.Floor(decade / (double)MetricPrefixes.ExponentStep)
            * MetricPrefixes.ExponentStep;

        return Math.Clamp(
            exponent, MetricPrefixes.SmallestExponent, MetricPrefixes.LargestExponent);
    }

    private static string FormatMantissa(double mantissa, int digits)
    {
        var magnitude = (int)Math.Floor(Math.Log10(Math.Abs(mantissa)));
        var decimals = Math.Clamp(digits - 1 - magnitude, 0, MaximumDecimals);
        var text = mantissa.ToString(
            "F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        return text.Contains('.') ? text.TrimEnd('0').TrimEnd('.') : text;
    }

    private static double Rescale(double value, int exponent) =>
        exponent >= 0 ? value * Math.Pow(10, exponent) : value / Math.Pow(10, -exponent);
}
