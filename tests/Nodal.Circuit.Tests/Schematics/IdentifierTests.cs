using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Schematics;

public class IdentifierTests
{
    [Fact]
    public void TheFirstComponentIdIsOne() => Assert.Equal(1, ComponentId.First.Value);

    [Fact]
    public void TheFirstWireIdIsOne() => Assert.Equal(1, WireId.First.Value);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(7, 8)]
    [InlineData(-1, 0)]
    public void TheNextComponentIdCountsUpByOne(int value, int expected) =>
        Assert.Equal(new ComponentId(expected), new ComponentId(value).Next());

    [Theory]
    [InlineData(1, 2)]
    [InlineData(7, 8)]
    public void TheNextWireIdCountsUpByOne(int value, int expected) =>
        Assert.Equal(new WireId(expected), new WireId(value).Next());

    [Fact]
    public void ComponentIdsCompareByValue()
    {
        Assert.True(new ComponentId(2).CompareTo(new ComponentId(3)) < 0);
        Assert.True(new ComponentId(3).CompareTo(new ComponentId(2)) > 0);
        Assert.Equal(0, new ComponentId(3).CompareTo(new ComponentId(3)));
    }

    [Fact]
    public void WireIdsCompareByValue()
    {
        Assert.True(new WireId(2).CompareTo(new WireId(3)) < 0);
        Assert.True(new WireId(3).CompareTo(new WireId(2)) > 0);
        Assert.Equal(0, new WireId(3).CompareTo(new WireId(3)));
    }

    [Fact]
    public void IdentifierTextIsTheNumber()
    {
        Assert.Equal("12", new ComponentId(12).ToString());
        Assert.Equal("12", new WireId(12).ToString());
    }

    [Fact]
    public void IdentifiersWithTheSameValueAreEqual()
    {
        Assert.Equal(new ComponentId(5), new ComponentId(5));
        Assert.NotEqual(new ComponentId(5), new ComponentId(6));
        Assert.Equal(new WireId(5), new WireId(5));
        Assert.NotEqual(new WireId(5), new WireId(6));
    }
}
