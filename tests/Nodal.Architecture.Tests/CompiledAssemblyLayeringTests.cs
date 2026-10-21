using Xunit;

namespace Nodal.Architecture.Tests;

public class CompiledAssemblyLayeringTests
{
    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    [InlineData(CoreLayers.Editor)]
    public void CoreAssemblyReferencesNoGodotAssembly(string layerName)
    {
        var godotReferences = CoreAssemblies.ReferencedAssemblyNames(layerName)
            .Where(CoreLayers.IsGodotAssemblyName)
            .ToArray();

        Assert.Empty(godotReferences);
    }

    [Theory]
    [InlineData(CoreLayers.Foundation)]
    [InlineData(CoreLayers.Content)]
    [InlineData(CoreLayers.Circuit)]
    [InlineData(CoreLayers.Editor)]
    public void CoreAssemblyReferencesOnlyAllowedNodalAssemblies(string layerName)
    {
        var allowed = CoreLayers.AllowedNodalDependenciesOf(layerName);

        var forbidden = CoreAssemblies.ReferencedAssemblyNames(layerName)
            .Where(CoreLayers.IsNodalAssemblyName)
            .Where(name => name != layerName)
            .Except(allowed)
            .ToArray();

        Assert.Empty(forbidden);
    }
}
