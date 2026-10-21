using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Geometry;

public readonly record struct GridBounds(GridPoint Minimum, GridPoint Maximum)
{
    public int Width => Maximum.X - Minimum.X;

    public int Height => Maximum.Y - Minimum.Y;

    public GridVector Center => new((Minimum.X + Maximum.X) / 2.0, (Minimum.Y + Maximum.Y) / 2.0);

    public static GridBounds Around(IEnumerable<GridPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var minimumX = int.MaxValue;
        var minimumY = int.MaxValue;
        var maximumX = int.MinValue;
        var maximumY = int.MinValue;

        foreach (var point in points)
        {
            minimumX = Math.Min(minimumX, point.X);
            minimumY = Math.Min(minimumY, point.Y);
            maximumX = Math.Max(maximumX, point.X);
            maximumY = Math.Max(maximumY, point.Y);
        }

        if (minimumX > maximumX)
        {
            throw new ArgumentException("Bounds need at least one point.", nameof(points));
        }

        return new GridBounds(new GridPoint(minimumX, minimumY), new GridPoint(maximumX, maximumY));
    }

    public GridBounds Grown(int margin) => new(
        new GridPoint(Minimum.X - margin, Minimum.Y - margin),
        new GridPoint(Maximum.X + margin, Maximum.Y + margin));

    public override string ToString() => $"{Minimum} to {Maximum}";
}
