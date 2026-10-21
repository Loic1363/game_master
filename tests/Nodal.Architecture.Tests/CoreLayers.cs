namespace Nodal.Architecture.Tests;

internal static class CoreLayers
{
    public const string Foundation = "Nodal.Foundation";
    public const string Content = "Nodal.Content";
    public const string Circuit = "Nodal.Circuit";

    private static readonly Dictionary<string, string[]> AllowedNodalDependenciesByLayer = new()
    {
        [Foundation] = Array.Empty<string>(),
        [Content] = new[] { Foundation },
        [Circuit] = new[] { Foundation, Content },
    };

    public static IReadOnlyCollection<string> AllowedNodalDependenciesOf(string layerName) =>
        AllowedNodalDependenciesByLayer.TryGetValue(layerName, out var allowed)
            ? allowed
            : throw new InvalidOperationException($"{layerName} is not a known core layer.");

    public static bool IsGodotAssemblyName(string name) =>
        name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase);

    public static bool IsNodalAssemblyName(string name) =>
        name.StartsWith("Nodal.", StringComparison.Ordinal);
}
