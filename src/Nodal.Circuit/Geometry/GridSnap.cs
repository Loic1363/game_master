using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Geometry;

public static class GridSnap
{
    public static GridPoint Nearest(GridVector point) => new(
        (int)Math.Round(point.X, MidpointRounding.AwayFromZero),
        (int)Math.Round(point.Y, MidpointRounding.AwayFromZero));
}
