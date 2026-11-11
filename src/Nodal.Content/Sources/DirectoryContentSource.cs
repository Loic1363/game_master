using Nodal.Content.Components;
using Nodal.Content.Diagnostics;

namespace Nodal.Content.Sources;

public sealed class DirectoryContentSource : IContentSource
{
    private const string JsonSearchPattern = "*.json";

    private readonly string _directory;
    private readonly List<ContentDiagnostic> _diagnostics = [];

    public DirectoryContentSource(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        _directory = directory;
    }

    public IReadOnlyList<ContentDiagnostic> Diagnostics => _diagnostics;

    public IEnumerable<ContentFile> ReadAll()
    {
        _diagnostics.Clear();

        foreach (var path in OrderedFilePaths())
        {
            if (TryReadText(path, out var text))
            {
                yield return new ContentFile(Path.GetFileName(path), text);
            }
        }
    }

    private static bool IsFileFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;

    private static string ReasonOf(Exception failure) => failure.Message.TrimEnd('.');

    private IReadOnlyList<string> OrderedFilePaths()
    {
        try
        {
            return Directory.EnumerateFiles(_directory, JsonSearchPattern)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            Report(_directory, exception);

            return [];
        }
    }

    private bool TryReadText(string path, out string text)
    {
        try
        {
            text = File.ReadAllText(path);

            return true;
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            Report(path, exception);
            text = string.Empty;

            return false;
        }
    }

    private void Report(string path, Exception failure) =>
        _diagnostics.Add(ContentDiagnostic.Error(
            Path.GetFileName(path),
            JsonPath.Root,
            $"Could not read content file {path}: {ReasonOf(failure)}."));
}
