using Nodal.Circuit.Geometry;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Geometry;

public class GridSnapTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1.4, -1.4, 1, -1)]
    [InlineData(1.6, -1.6, 2, -2)]
    [InlineData(0.5, -0.5, 1, -1)]
    [InlineData(-2.5, 2.5, -3, 3)]
    public void PointsSnapToTheNearestGridPoint(double x, double y, int snappedX, int snappedY) =>
        Assert.Equal(new GridPoint(snappedX, snappedY), GridSnap.Nearest(new GridVector(x, y)));
}
