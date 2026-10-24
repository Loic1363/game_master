using Xunit;

namespace Nodal.Architecture.Tests;

public class SimulationLayerTests
{
    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    public void LayersBelowSimulationDoNotReferenceItsAssembly(string layerName) =>
        Assert.DoesNotContain(
            CoreLayers.Simulation, CoreAssemblies.ReferencedAssemblyNames(layerName));

    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    public void LayersBelowSimulationDoNotReferenceItsProject(string layerName) =>
        Assert.DoesNotContain(
            CoreLayers.Simulation,
            ProjectFile.ProjectReferenceNames(RepositoryPaths.CoreProjectFile(layerName)));

    [Fact]
    public void SimulationAssemblyReferencesTheCircuitLayer() => Assert.Contains(
        CoreLayers.Circuit, CoreAssemblies.ReferencedAssemblyNames(CoreLayers.Simulation));

    [Fact]
    public void SimulationDoesNotReferenceTheEditorLayer() => Assert.DoesNotContain(
        CoreLayers.Editor, CoreAssemblies.ReferencedAssemblyNames(CoreLayers.Simulation));

    [Fact]
    public void EditorProjectMayReferenceSimulation() => Assert.Contains(
        CoreLayers.Simulation,
        ProjectFile.ProjectReferenceNames(RepositoryPaths.CoreProjectFile(CoreLayers.Editor)));

    [Fact]
    public void GameProjectReferencesSimulation() => Assert.Contains(
        CoreLayers.Simulation, ProjectFile.ProjectReferenceNames(RepositoryPaths.GameProjectFile));

    [Fact]
    public void SimulationProjectTargetsTheFrameworkPropertyDeclaredInDirectoryBuildProps() =>
        Assert.Equal(
            "$(NodalTargetFramework)",
            ProjectFile.PropertyValue(
                RepositoryPaths.CoreProjectFile(CoreLayers.Simulation), "TargetFramework"));
}
