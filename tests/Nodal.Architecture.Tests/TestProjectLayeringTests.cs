using Xunit;

namespace Nodal.Architecture.Tests;

public class TestProjectLayeringTests
{
    [Theory]
    [InlineData(TestProjects.Foundation)]
    [InlineData(TestProjects.Content)]
    [InlineData(TestProjects.Circuit)]
    [InlineData(TestProjects.Simulation)]
    [InlineData(TestProjects.Editor)]
    [InlineData(TestProjects.Architecture)]
    public void TestProjectDeclaresNoGodotReference(string projectName)
    {
        var projectFile = RepositoryPaths.TestProjectFile(projectName);

        var godotReferences = ProjectFile.PackageReferenceNames(projectFile)
            .Concat(ProjectFile.ProjectReferenceNames(projectFile))
            .Where(CoreLayers.IsGodotAssemblyName)
            .ToArray();

        Assert.Empty(godotReferences);
    }

    [Theory]
    [InlineData(TestProjects.Foundation)]
    [InlineData(TestProjects.Content)]
    [InlineData(TestProjects.Circuit)]
    [InlineData(TestProjects.Simulation)]
    [InlineData(TestProjects.Editor)]
    [InlineData(TestProjects.Architecture)]
    public void TestProjectDeclaresOnlyAllowedNodalProjectReferences(string projectName)
    {
        var allowed = TestProjects.AllowedNodalReferencesOf(projectName);

        var forbidden = ProjectFile.ProjectReferenceNames(RepositoryPaths.TestProjectFile(projectName))
            .Where(CoreLayers.IsNodalAssemblyName)
            .Except(allowed)
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Theory]
    [InlineData(TestProjects.Foundation)]
    [InlineData(TestProjects.Content)]
    [InlineData(TestProjects.Circuit)]
    [InlineData(TestProjects.Simulation)]
    [InlineData(TestProjects.Editor)]
    [InlineData(TestProjects.Architecture)]
    public void TestProjectTargetsTheFrameworkPropertyDeclaredInDirectoryBuildProps(string projectName)
    {
        var targetFramework = ProjectFile.PropertyValue(
            RepositoryPaths.TestProjectFile(projectName), "TargetFramework");

        Assert.Equal("$(NodalTargetFramework)", targetFramework);
    }
}
