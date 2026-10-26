using System.Globalization;

namespace Nodal.Simulation.Spice;

internal static class SpiceFormat
{
    public const string LineSeparator = "\n";
    public const string GroundNode = "0";
    public const string EsrSuffix = "_esr";
    public const string EslSuffix = "_esl";
    public const string ParallelSuffix = "_par";
    public const string DiodeModelName = "NODAL_D";
    public const string ModelLine = ".model NODAL_D D(IS=1e-14 N=1)";
    public const string OptionsLine = ".options TNOM=27";
    public const string EndLine = ".end";
    public const string TitlePrefix = "* ";
    public const string TitleSuffix = " export";

    public static string Value(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    public static string Count(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    public static string Name(string designator, char type, string suffix) =>
        (designator.StartsWith(type.ToString(), StringComparison.OrdinalIgnoreCase)
            ? designator
            : type + designator) + suffix;
}
