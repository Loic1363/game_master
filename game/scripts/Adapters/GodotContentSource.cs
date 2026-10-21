using Godot;
using Nodal.Content.Sources;
using FileAccess = Godot.FileAccess;

namespace Nodal.Game.Adapters;

public sealed class GodotContentSource : IContentSource
{
    private const string JsonExtension = ".json";

    private readonly string _directoryPath;

    public GodotContentSource(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        _directoryPath = directoryPath;
    }

    public IEnumerable<ContentFile> ReadAll()
    {
        foreach (var fileName in OrderedJsonFileNames())
        {
            using var file = FileAccess.Open(_directoryPath.PathJoin(fileName), FileAccess.ModeFlags.Read);

            if (file is null)
            {
                GD.PrintErr($"Could not open {_directoryPath.PathJoin(fileName)}: {FileAccess.GetOpenError()}.");
                continue;
            }

            yield return new ContentFile(fileName, file.GetAsText());
        }
    }

    private IReadOnlyList<string> OrderedJsonFileNames()
    {
        using var directory = DirAccess.Open(_directoryPath);

        if (directory is null)
        {
            GD.PrintErr($"Could not open {_directoryPath}: {DirAccess.GetOpenError()}.");
            return [];
        }

        return directory.GetFiles()
            .Where(IsJsonFileName)
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsJsonFileName(string fileName) =>
        fileName.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase);
}
