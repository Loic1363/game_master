namespace Nodal.Content.Diagnostics;

public sealed record ContentDiagnostic(
    ContentDiagnosticSeverity Severity, string SourceFile, string Path, string Message)
{
    public static ContentDiagnostic Error(string sourceFile, string path, string message) =>
        new(ContentDiagnosticSeverity.Error, sourceFile, path, message);

    public static ContentDiagnostic Warning(string sourceFile, string path, string message) =>
        new(ContentDiagnosticSeverity.Warning, sourceFile, path, message);

    public override string ToString() => $"{Severity}: {SourceFile} {Path}: {Message}";
}
