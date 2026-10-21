using System.Globalization;

namespace Nodal.Foundation.Geometry;

public readonly record struct GridVector(double X, double Y)
{
    public static GridVector Zero { get; } = new(0, 0);

    public double Length => Math.Sqrt((X * X) + (Y * Y));

    public static GridVector operator +(GridVector left, GridVector right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static GridVector operator -(GridVector left, GridVector right) =>
        new(left.X - right.X, left.Y - right.Y);

    public static GridVector operator *(GridVector vector, double factor) =>
        new(vector.X * factor, vector.Y * factor);

    public double Dot(GridVector other) => (X * other.X) + (Y * other.Y);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y})");
}
