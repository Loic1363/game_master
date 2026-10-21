using Xunit;

namespace Nodal.Architecture.Tests;

public class GameProjectFrameworkTests
{
    [Fact]
    public void GameProjectTargetsTheFrameworkDeclaredInDirectoryBuildProps()
    {
        var declaredFramework = ProjectFile.PropertyValue(
            RepositoryPaths.DirectoryBuildPropsFile, "NodalTargetFramework");

        var gameFramework = ProjectFile.PropertyValue(
            RepositoryPaths.GameProjectFile, "TargetFramework");

        Assert.Equal(declaredFramework, gameFramework);
    }
}
