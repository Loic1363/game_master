namespace Nodal.Circuit.Persistence;

public sealed record SchematicDiagnostic(SchematicSeverity Severity, string Path, string Message)
{
    public static SchematicDiagnostic Error(string path, string message) =>
        new(SchematicSeverity.Error, path, message);

    public static SchematicDiagnostic Warning(string path, string message) =>
        new(SchematicSeverity.Warning, path, message);

    public override string ToString() => $"{Severity}: {Path}: {Message}";
}
