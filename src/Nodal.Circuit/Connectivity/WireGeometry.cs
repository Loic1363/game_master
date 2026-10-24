using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Connectivity;

public static class WireGeometry
{
    public static bool Contains(Wire wire, GridPoint point) =>
        wire.IsHorizontal
            ? point.Y == wire.Start.Y && IsBetween(point.X, wire.Start.X, wire.End.X)
            : point.X == wire.Start.X && IsBetween(point.Y, wire.Start.Y, wire.End.Y);

    public static bool ContainsInInterior(Wire wire, GridPoint point) =>
        point != wire.Start && point != wire.End && Contains(wire, point);

    private static bool IsBetween(int value, int first, int second) =>
        value >= Math.Min(first, second) && value <= Math.Max(first, second);
}
