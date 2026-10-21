using System.Globalization;

namespace Nodal.Foundation.Quantities;

internal static class QuantityFormatter
{
    private const int SignificantDigits = 3;
    private const string MantissaOverflowText = "1000";

    public static string Format(double value, Unit unit)
    {
        var (mantissaText, exponent) = ToEngineering(value);
        var symbol = MetricPrefixes.SymbolFor(exponent) + unit.Symbol();

        return symbol.Length == 0 ? mantissaText : mantissaText + " " + symbol;
    }

    private static (string MantissaText, int Exponent) ToEngineering(double value)
    {
        if (value == 0)
        {
            return ("0", 0);
        }

        if (!double.IsFinite(value))
        {
            return (value.ToString(CultureInfo.InvariantCulture), 0);
        }

        var exponent = EngineeringExponent(value);
        var mantissaText = FormatMantissa(Rescale(value, -exponent));

        if (mantissaText.TrimStart('-') == MantissaOverflowText && exponent < MetricPrefixes.LargestExponent)
        {
            exponent += MetricPrefixes.ExponentStep;
            mantissaText = FormatMantissa(Rescale(value, -exponent));
        }

        return (mantissaText, exponent);
    }

    private static int EngineeringExponent(double value)
    {
        var decade = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        var exponent = (int)Math.Floor(decade / (double)MetricPrefixes.ExponentStep) * MetricPrefixes.ExponentStep;

        return Math.Clamp(exponent, MetricPrefixes.SmallestExponent, MetricPrefixes.LargestExponent);
    }

    private static string FormatMantissa(double mantissa)
    {
        if (mantissa == 0)
        {
            return "0";
        }

        var magnitude = (int)Math.Floor(Math.Log10(Math.Abs(mantissa)));
        var decimals = Math.Clamp(SignificantDigits - 1 - magnitude, 0, 15);
        var text = mantissa.ToString(
            "F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        return text.Contains('.') ? text.TrimEnd('0').TrimEnd('.') : text;
    }

    private static double Rescale(double value, int exponent) =>
        exponent >= 0 ? value * Math.Pow(10, exponent) : value / Math.Pow(10, -exponent);
}
