using System.Globalization;

namespace Nodal.Circuit.Schematics;

public static class SchematicLimits
{
    public const long MaxFileBytes = 16 * BytesPerMegabyte;
    public const int MaxComponents = 20000;
    public const int MaxWires = 100000;
    public const int MaxDiagnostics = 200;
    public const double MaxTransientPoints = 1e6;
    public const double SmallestMaxStepFraction = 1e-9;

    private const long BytesPerMegabyte = 1024 * 1024;
    private const string MegabyteFormat = "F1";
    private const string CountFormat = "N0";

    public static string MaxFileMegabytesText =>
        CountText(MaxFileBytes / BytesPerMegabyte);

    public static string MegabytesText(long bytes) =>
        (bytes / (double)BytesPerMegabyte).ToString(MegabyteFormat, CultureInfo.InvariantCulture);

    public static string CountText(long value) =>
        value.ToString(CountFormat, CultureInfo.InvariantCulture);
}
