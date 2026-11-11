using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Persistence;

public static class SchematicFile
{
    public const string Extension = ".nodal";

    public const long MaxFileBytes = SchematicLimits.MaxFileBytes;

    public static string? FileSizeProblem(long lengthInBytes) =>
        lengthInBytes > MaxFileBytes
            ? $"The file is too large to open: {SchematicLimits.MegabytesText(lengthInBytes)} MB, "
                + $"while the limit is {SchematicLimits.MaxFileMegabytesText} MB."
            : null;

    public static string Write(Schematic schematic)
    {
        ArgumentNullException.ThrowIfNull(schematic);

        return SchematicWriter.Write(schematic);
    }

    public static SchematicReadResult Read(string text, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(catalog);

        return SchematicReader.Read(text, catalog);
    }
}
