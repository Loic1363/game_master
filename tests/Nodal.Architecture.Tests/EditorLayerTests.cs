using Xunit;

namespace Nodal.Architecture.Tests;

public class EditorLayerTests
{
    [Fact]
    public void CircuitAssemblyDoesNotReferenceTheEditorLayer() => Assert.DoesNotContain(
        CoreLayers.Editor, CoreAssemblies.ReferencedAssemblyNames(CoreLayers.Circuit));

    [Fact]
    public void CircuitProjectDoesNotReferenceTheEditorLayer() => Assert.DoesNotContain(
        CoreLayers.Editor,
        ProjectFile.ProjectReferenceNames(RepositoryPaths.CoreProjectFile(CoreLayers.Circuit)));

    [Fact]
    public void FoundationAndContentDoNotReferenceTheEditorLayer()
    {
        Assert.DoesNotContain(
            CoreLayers.Editor, CoreAssemblies.ReferencedAssemblyNames(CoreLayers.Foundation));
        Assert.DoesNotContain(
            CoreLayers.Editor, CoreAssemblies.ReferencedAssemblyNames(CoreLayers.Content));
    }

    [Fact]
    public void EditorAssemblyReferencesTheCircuitLayer() => Assert.Contains(
        CoreLayers.Circuit, CoreAssemblies.ReferencedAssemblyNames(CoreLayers.Editor));

    [Fact]
    public void GameProjectReferencesTheEditorLayer() => Assert.Contains(
        CoreLayers.Editor, ProjectFile.ProjectReferenceNames(RepositoryPaths.GameProjectFile));

    [Fact]
    public void EditorProjectTargetsTheFrameworkPropertyDeclaredInDirectoryBuildProps() =>
        Assert.Equal(
            "$(NodalTargetFramework)",
            ProjectFile.PropertyValue(
                RepositoryPaths.CoreProjectFile(CoreLayers.Editor), "TargetFramework"));
}
