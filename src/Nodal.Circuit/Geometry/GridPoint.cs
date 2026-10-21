using System.Globalization;

namespace Nodal.Circuit.Geometry;

public readonly record struct GridPoint(int X, int Y)
{
    public static GridPoint Origin { get; } = new(0, 0);

    public static GridPoint operator +(GridPoint left, GridPoint right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static GridPoint operator -(GridPoint left, GridPoint right) =>
        new(left.X - right.X, left.Y - right.Y);

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture, $"({X}, {Y})");
}
