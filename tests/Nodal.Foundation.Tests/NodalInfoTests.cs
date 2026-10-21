using Xunit;

namespace Nodal.Foundation.Tests;

public class NodalInfoTests
{
    [Fact]
    public void NameIsNodal()
    {
        Assert.Equal("Nodal", NodalInfo.Name);
    }

    [Fact]
    public void VersionMatchesDirectoryBuildProps()
    {
        Assert.Equal(DeclaredVersion.FromDirectoryBuildProps, NodalInfo.Version);
    }
}
