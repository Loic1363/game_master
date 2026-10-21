namespace Nodal.Architecture.Tests;

internal static class RepositoryPaths
{
    private const string SolutionFileName = "Nodal.sln";

    public static string Root { get; } = FindRoot();

    public static string CoreProjectFile(string projectName) =>
        Path.Combine(Root, "src", projectName, $"{projectName}.csproj");

    public static string TestProjectFile(string projectName) =>
        Path.Combine(Root, "tests", projectName, $"{projectName}.csproj");

    public static string GameProjectFile => Path.Combine(Root, "game", "Nodal.Game.csproj");

    public static string DirectoryBuildPropsFile => Path.Combine(Root, "Directory.Build.props");

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
