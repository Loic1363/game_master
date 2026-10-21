namespace Nodal.Circuit.Geometry;

public readonly record struct Orientation(Rotation Rotation, bool Mirrored)
{
    private const string MirroredSuffix = " mirrored";

    public static Orientation Default { get; } = new(Geometry.Rotation.R0, false);

    public GridPoint Transform(GridPoint offset)
    {
        var point = Mirrored ? new GridPoint(-offset.X, offset.Y) : offset;

        for (var turn = 0; turn < Rotation.QuarterTurns(); turn++)
        {
            point = new GridPoint(-point.Y, point.X);
        }

        return point;
    }

    public Orientation RotatedClockwise() => new(Rotation.NextClockwise(), Mirrored);

    public Orientation Flipped() => new(Rotation, !Mirrored);

    public override string ToString() =>
        Mirrored ? Rotation.ToString() + MirroredSuffix : Rotation.ToString();
}
