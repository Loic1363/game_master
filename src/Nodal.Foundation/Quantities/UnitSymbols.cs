namespace Nodal.Foundation.Quantities;

public static class UnitSymbols
{
    public const string Ohm = "Ω";

    private static readonly Dictionary<Unit, string> SymbolByUnit = new()
    {
        [Unit.Dimensionless] = string.Empty,
        [Unit.Ohm] = Ohm,
        [Unit.Farad] = "F",
        [Unit.Henry] = "H",
        [Unit.Volt] = "V",
        [Unit.Ampere] = "A",
        [Unit.Second] = "s",
        [Unit.Hertz] = "Hz",
        [Unit.Watt] = "W",
    };

    public static string Symbol(this Unit unit) =>
        SymbolByUnit.TryGetValue(unit, out var symbol)
            ? symbol
            : throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit.");
}
