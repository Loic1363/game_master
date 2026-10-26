using Godot;
using Nodal.Circuit.Persistence;
using Nodal.Foundation;

namespace Nodal.Game.Adapters;

public static class SchematicFiles
{
    private const string TemporarySuffix = ".tmp";

    public static string DefaultDirectory()
    {
        var documents = OS.GetSystemDir(OS.SystemDir.Documents);
        var directory = documents.PathJoin(NodalInfo.Name);

        try
        {
            Directory.CreateDirectory(directory);

            return directory;
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            GD.PrintErr($"Could not create {directory}: {exception.Message}");

            return documents;
        }
    }

    public static string WithExtension(string path) =>
        WithExtension(path, SchematicFile.Extension);

    public static string WithExtension(string path, string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? path
            : path + extension;
    }

    public static bool TryRead(string path, out string text, out string error)
    {
        try
        {
            text = File.ReadAllText(path);
            error = string.Empty;

            return true;
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            text = string.Empty;
            error = exception.Message;

            return false;
        }
    }

    public static bool TryWrite(string path, string text, out string error)
    {
        var temporary = path + TemporarySuffix;

        try
        {
            File.WriteAllText(temporary, text);
            File.Move(temporary, path, overwrite: true);
            error = string.Empty;

            return true;
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            Discard(temporary);
            error = exception.Message;

            return false;
        }
    }

    private static void Discard(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            GD.PrintErr($"Could not delete {path}: {exception.Message}");
        }
    }

    private static bool IsFileFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException;
}
