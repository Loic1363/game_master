using Nodal.Foundation;

namespace Nodal.Game.Presentation;

public sealed class SchematicDocument
{
    private const string UntitledName = "Untitled";
    private const string TitleSeparator = " — ";
    private const string DirtyMarker = "*";

    public string? FilePath { get; private set; }

    public bool IsUntitled => FilePath is null;

    public string Name => FilePath is null ? UntitledName : Path.GetFileName(FilePath);

    public void Track(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FilePath = filePath;
    }

    public void Reset() => FilePath = null;

    public string Title(bool dirty) =>
        NodalInfo.Name + TitleSeparator + Name + (dirty ? DirtyMarker : string.Empty);
}
