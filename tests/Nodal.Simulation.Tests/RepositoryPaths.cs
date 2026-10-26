namespace Nodal.Simulation.Tests;

internal static class RepositoryPaths
{
    private const string SolutionFileName = "Nodal.sln";

    public static string Root { get; } = FindRoot();

    public static string ComponentsDirectory => Path.Combine(Root, "game", "content", "components");

    public static string ReferenceCircuitsDirectory =>
        Path.Combine(Root, "tests", "reference-circuits");

    public static string SpiceDirectory =>
        Path.Combine(Root, "tests", "Nodal.Simulation.Tests", "Spice");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException(
            $"Could not find {SolutionFileName} in any directory above {AppContext.BaseDirectory}.");
    }
}
