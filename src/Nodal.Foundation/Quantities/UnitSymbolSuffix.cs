namespace Nodal.Foundation.Quantities;

internal static class UnitSymbolSuffix
{
    private static readonly (string Text, Unit Unit, StringComparison Comparison)[] LongestFirst =
    [
        ("ohms", Unit.Ohm, StringComparison.OrdinalIgnoreCase),
        ("ohm", Unit.Ohm, StringComparison.OrdinalIgnoreCase),
        ("Hz", Unit.Hertz, StringComparison.Ordinal),
        (UnitSymbols.Ohm, Unit.Ohm, StringComparison.Ordinal),
        ("F", Unit.Farad, StringComparison.Ordinal),
        ("H", Unit.Henry, StringComparison.Ordinal),
        ("V", Unit.Volt, StringComparison.Ordinal),
        ("A", Unit.Ampere, StringComparison.Ordinal),
        ("s", Unit.Second, StringComparison.Ordinal),
        ("W", Unit.Watt, StringComparison.Ordinal),
    ];

    public static bool TryMatch(string text, out Unit unit, out int length)
    {
        foreach (var (candidate, candidateUnit, comparison) in LongestFirst)
        {
            if (text.EndsWith(candidate, comparison))
            {
                unit = candidateUnit;
                length = candidate.Length;
                return true;
            }
        }

        unit = Unit.Dimensionless;
        length = 0;
        return false;
    }
}
