using Nodal.Foundation.Collections;
using Xunit;

namespace Nodal.Foundation.Tests.Collections;

public class DisjointSetTests
{
    [Fact]
    public void EveryElementStartsInItsOwnClass()
    {
        var set = new DisjointSet(3);

        Assert.Equal(3, set.Count);
        Assert.Equal(0, set.Find(0));
        Assert.Equal(1, set.Find(1));
        Assert.False(set.AreJoined(0, 1));
    }

    [Fact]
    public void JoinedElementsShareARoot()
    {
        var set = new DisjointSet(4);

        set.Union(0, 1);
        set.Union(2, 3);

        Assert.True(set.AreJoined(0, 1));
        Assert.True(set.AreJoined(2, 3));
        Assert.False(set.AreJoined(1, 2));
    }

    [Fact]
    public void JoiningTwoClassesMergesAllTheirMembers()
    {
        var set = new DisjointSet(4);

        set.Union(0, 1);
        set.Union(2, 3);
        set.Union(1, 2);

        Assert.True(set.AreJoined(0, 3));
        Assert.Equal(set.Find(0), set.Find(3));
    }

    [Fact]
    public void JoiningTheSameClassTwiceChangesNothing()
    {
        var set = new DisjointSet(2);

        set.Union(0, 1);
        set.Union(1, 0);

        Assert.True(set.AreJoined(0, 1));
    }

    [Fact]
    public void ALongChainStillResolvesToOneRoot()
    {
        var set = new DisjointSet(100);

        for (var index = 1; index < 100; index++)
        {
            set.Union(index - 1, index);
        }

        Assert.True(set.AreJoined(0, 99));
    }

    [Fact]
    public void AnEmptySetHoldsNothing()
    {
        Assert.Equal(0, new DisjointSet(0).Count);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AnElementOutsideTheSetIsRejected(int index)
    {
        var set = new DisjointSet(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => set.Find(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.Union(0, index));
    }

    [Fact]
    public void ANegativeCountIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DisjointSet(-1));
    }
}
