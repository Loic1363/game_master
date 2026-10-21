using Xunit;

namespace Nodal.Architecture.Tests;

public class ProjectFileLayeringTests
{
    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    [InlineData(CoreLayers.Editor)]
    public void CoreProjectDeclaresNoGodotReference(string layerName)
    {
        var projectFile = RepositoryPaths.CoreProjectFile(layerName);

        var godotReferences = ProjectFile.PackageReferenceNames(projectFile)
            .Concat(ProjectFile.ProjectReferenceNames(projectFile))
            .Where(CoreLayers.IsGodotAssemblyName)
            .ToArray();

        Assert.Empty(godotReferences);
    }

    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    [InlineData(CoreLayers.Editor)]
    public void CoreProjectDeclaresOnlyAllowedNodalProjectReferences(string layerName)
    {
        var allowed = CoreLayers.AllowedNodalDependenciesOf(layerName);

        var forbidden = ProjectFile.ProjectReferenceNames(RepositoryPaths.CoreProjectFile(layerName))
            .Where(CoreLayers.IsNodalAssemblyName)
            .Except(allowed)
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    [InlineData(CoreLayers.Editor)]
    public void CoreProjectDeclaresNoNonNodalProjectReference(string layerName)
    {
        var unexpected = ProjectFile.ProjectReferenceNames(RepositoryPaths.CoreProjectFile(layerName))
            .Where(name => !CoreLayers.IsNodalAssemblyName(name))
            .ToArray();

        Assert.Empty(unexpected);
    }
}
