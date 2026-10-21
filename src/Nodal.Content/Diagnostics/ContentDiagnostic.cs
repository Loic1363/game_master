namespace Nodal.Content.Diagnostics;

public sealed record ContentDiagnostic(
    ContentDiagnosticSeverity Severity, string SourceFile, string Path, string Message)
{
    public static ContentDiagnostic Error(string sourceFile, string path, string message) =>
        new(ContentDiagnosticSeverity.Error, sourceFile, path, message);

    public override string ToString() => $"{Severity}: {SourceFile} {Path}: {Message}";
}
