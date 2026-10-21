namespace Nodal.Circuit.Geometry;

public static class Rotations
{
    public const int QuarterTurnsPerTurn = 4;

    public static int QuarterTurns(this Rotation rotation) => rotation switch
    {
        Rotation.R0 => 0,
        Rotation.R90 => 1,
        Rotation.R180 => 2,
        Rotation.R270 => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unknown rotation."),
    };

    public static Rotation NextClockwise(this Rotation rotation) =>
        FromQuarterTurns(rotation.QuarterTurns() + 1);

    public static Rotation FromQuarterTurns(int quarterTurns)
    {
        var normalized = ((quarterTurns % QuarterTurnsPerTurn) + QuarterTurnsPerTurn) % QuarterTurnsPerTurn;

        return normalized switch
        {
            0 => Rotation.R0,
            1 => Rotation.R90,
            2 => Rotation.R180,
            _ => Rotation.R270,
        };
    }
}
