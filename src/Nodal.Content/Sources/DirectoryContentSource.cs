namespace Nodal.Content.Sources;

public sealed class DirectoryContentSource : IContentSource
{
    private const string JsonSearchPattern = "*.json";

    private readonly string _directory;

    public DirectoryContentSource(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        _directory = directory;
    }

    public IEnumerable<ContentFile> ReadAll()
    {
        foreach (var path in OrderedFilePaths())
        {
            yield return new ContentFile(Path.GetFileName(path), File.ReadAllText(path));
        }
    }

    private IReadOnlyList<string> OrderedFilePaths() =>
        Directory.EnumerateFiles(_directory, JsonSearchPattern)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
}
