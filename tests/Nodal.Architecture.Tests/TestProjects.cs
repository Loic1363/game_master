namespace Nodal.Architecture.Tests;

internal static class TestProjects
{
    public const string Foundation = "Nodal.Foundation.Tests";
    public const string Content = "Nodal.Content.Tests";
    public const string Circuit = "Nodal.Circuit.Tests";
    public const string Editor = "Nodal.Editor.Tests";
    public const string Architecture = "Nodal.Architecture.Tests";

    private static readonly Dictionary<string, string[]> AllowedNodalReferencesByProject = new()
    {
        [Foundation] = [CoreLayers.Foundation],
        [Content] = [CoreLayers.Foundation, CoreLayers.Content],
        [Circuit] = [CoreLayers.Foundation, CoreLayers.Content, CoreLayers.Circuit],
        [Editor] = [CoreLayers.Foundation, CoreLayers.Content, CoreLayers.Circuit, CoreLayers.Editor],
        [Architecture] = [CoreLayers.Foundation, CoreLayers.Content, CoreLayers.Circuit, CoreLayers.Editor],
    };

    public static IReadOnlyCollection<string> AllowedNodalReferencesOf(string projectName) =>
        AllowedNodalReferencesByProject.TryGetValue(projectName, out var allowed)
            ? allowed
            : throw new InvalidOperationException($"{projectName} is not a known test project.");
}
