using System.Globalization;
using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Commands;

internal static class Designators
{
    public static string Allocate(Schematic schematic, string prefix)
    {
        var used = new HashSet<int>();

        foreach (var component in schematic.Components.Values)
        {
            if (TryReadNumber(component.Designator, prefix, out var number))
            {
                used.Add(number);
            }
        }

        var next = 1;

        while (used.Contains(next))
        {
            next++;
        }

        return prefix + next.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryReadNumber(string designator, string prefix, out int number)
    {
        number = 0;

        if (!designator.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var digits = designator.AsSpan(prefix.Length);

        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var character in digits)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }
}
