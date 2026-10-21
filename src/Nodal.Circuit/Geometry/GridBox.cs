using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Geometry;

public readonly record struct GridBox(GridVector Minimum, GridVector Maximum)
{
    public double Width => Maximum.X - Minimum.X;

    public double Height => Maximum.Y - Minimum.Y;

    public static GridBox Around(IEnumerable<GridVector> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var minimumX = double.PositiveInfinity;
        var minimumY = double.PositiveInfinity;
        var maximumX = double.NegativeInfinity;
        var maximumY = double.NegativeInfinity;

        foreach (var point in points)
        {
            minimumX = Math.Min(minimumX, point.X);
            minimumY = Math.Min(minimumY, point.Y);
            maximumX = Math.Max(maximumX, point.X);
            maximumY = Math.Max(maximumY, point.Y);
        }

        if (minimumX > maximumX)
        {
            throw new ArgumentException("A box needs at least one point.", nameof(points));
        }

        return new GridBox(new GridVector(minimumX, minimumY), new GridVector(maximumX, maximumY));
    }

    public GridBox Grown(double margin) => new(
        new GridVector(Minimum.X - margin, Minimum.Y - margin),
        new GridVector(Maximum.X + margin, Maximum.Y + margin));

    public GridBox MovedBy(GridVector offset) => new(Minimum + offset, Maximum + offset);

    public bool Contains(GridVector point) =>
        point.X >= Minimum.X && point.X <= Maximum.X
        && point.Y >= Minimum.Y && point.Y <= Maximum.Y;

    public double DistanceTo(GridVector point)
    {
        var horizontal = Math.Max(0, Math.Max(Minimum.X - point.X, point.X - Maximum.X));
        var vertical = Math.Max(0, Math.Max(Minimum.Y - point.Y, point.Y - Maximum.Y));

        return new GridVector(horizontal, vertical).Length;
    }

    public override string ToString() => $"{Minimum} to {Maximum}";
}
