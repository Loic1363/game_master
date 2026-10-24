namespace Nodal.Circuit.Persistence;

public sealed record SchematicDiagnostic(
    SchematicSeverity Severity, string Path, string Message, string? Subject = null)
{
    public string Location => Subject ?? Path;

    public static SchematicDiagnostic Error(string path, string message) =>
        new(SchematicSeverity.Error, path, message);

    public static SchematicDiagnostic Warning(string path, string message) =>
        new(SchematicSeverity.Warning, path, message);

    public static SchematicDiagnostic Warning(string path, string? subject, string message) =>
        new(SchematicSeverity.Warning, path, message, subject);

    public override string ToString() => $"{Severity}: {Location}: {Message}";
}
