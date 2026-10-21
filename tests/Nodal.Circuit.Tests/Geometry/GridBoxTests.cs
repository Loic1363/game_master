using Nodal.Circuit.Geometry;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Geometry;

public class GridBoxTests
{
    [Fact]
    public void BoxAroundPointsSpansThemAll()
    {
        var box = GridBox.Around([new GridVector(1, 5), new GridVector(-2, 0.5), new GridVector(3, 2)]);

        Assert.Equal(new GridVector(-2, 0.5), box.Minimum);
        Assert.Equal(new GridVector(3, 5), box.Maximum);
        Assert.Equal(5, box.Width);
        Assert.Equal(4.5, box.Height);
    }

    [Fact]
    public void BoxAroundNoPointIsRejected() =>
        Assert.Throws<ArgumentException>(() => GridBox.Around([]));

    [Fact]
    public void BoxAroundNullIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => GridBox.Around(null!));

    [Fact]
    public void ContainsAcceptsThePointsOnTheEdge()
    {
        var box = new GridBox(new GridVector(-1, -1), new GridVector(1, 1));

        Assert.True(box.Contains(GridVector.Zero));
        Assert.True(box.Contains(new GridVector(1, -1)));
        Assert.False(box.Contains(new GridVector(1.01, 0)));
    }

    [Fact]
    public void DistanceIsZeroInsideAndPositiveOutside()
    {
        var box = new GridBox(new GridVector(-1, -1), new GridVector(1, 1));

        Assert.Equal(0, box.DistanceTo(GridVector.Zero));
        Assert.Equal(0.5, box.DistanceTo(new GridVector(1.5, 0)), 9);
        Assert.Equal(Math.Sqrt(2), box.DistanceTo(new GridVector(2, 2)), 9);
    }

    [Fact]
    public void GrowingAndMovingShiftBothCorners()
    {
        var box = new GridBox(new GridVector(0, 0), new GridVector(2, 2));

        Assert.Equal(
            new GridBox(new GridVector(-0.5, -0.5), new GridVector(2.5, 2.5)), box.Grown(0.5));
        Assert.Equal(
            new GridBox(new GridVector(3, 1), new GridVector(5, 3)), box.MovedBy(new GridVector(3, 1)));
    }

    [Fact]
    public void BoxesWithEqualCornersAreEqual()
    {
        var box = new GridBox(GridVector.Zero, new GridVector(1, 1));

        Assert.Equal(box, new GridBox(GridVector.Zero, new GridVector(1, 1)));
        Assert.Equal("(0, 0) to (1, 1)", box.ToString());
    }
}
