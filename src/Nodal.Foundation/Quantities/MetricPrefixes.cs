namespace Nodal.Foundation.Quantities;

internal static class MetricPrefixes
{
    public const char MicroSign = 'µ';
    public const char GreekSmallLetterMu = 'μ';
    public const string SpiceMega = "meg";
    public const int SmallestExponent = -15;
    public const int LargestExponent = 12;
    public const int ExponentStep = 3;

    private static readonly Dictionary<char, int> ExponentBySymbol = new()
    {
        ['f'] = -15,
        ['p'] = -12,
        ['n'] = -9,
        ['u'] = -6,
        [MicroSign] = -6,
        [GreekSmallLetterMu] = -6,
        ['m'] = -3,
        ['k'] = 3,
        ['M'] = 6,
        ['G'] = 9,
        ['T'] = 12,
    };

    private static readonly Dictionary<int, string> SymbolByExponent = new()
    {
        [-15] = "f",
        [-12] = "p",
        [-9] = "n",
        [-6] = MicroSign.ToString(),
        [-3] = "m",
        [0] = "",
        [3] = "k",
        [6] = "M",
        [9] = "G",
        [12] = "T",
    };

    public static bool TryExponent(char symbol, out int exponent) =>
        ExponentBySymbol.TryGetValue(symbol, out exponent);

    public static string SymbolFor(int exponent) =>
        SymbolByExponent.TryGetValue(exponent, out var symbol)
            ? symbol
            : throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "No metric prefix for this exponent.");
}
