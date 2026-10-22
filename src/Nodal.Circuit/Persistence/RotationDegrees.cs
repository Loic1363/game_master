using Nodal.Circuit.Geometry;

namespace Nodal.Circuit.Persistence;

internal static class RotationDegrees
{
    public const int PerQuarterTurn = 90;

    public static int Of(Rotation rotation) => rotation.QuarterTurns() * PerQuarterTurn;

    public static bool TryParse(int degrees, out Rotation rotation)
    {
        rotation = Geometry.Rotation.R0;

        if (degrees % PerQuarterTurn != 0 || degrees < 0
            || degrees >= PerQuarterTurn * Rotations.QuarterTurnsPerTurn)
        {
            return false;
        }

        rotation = Rotations.FromQuarterTurns(degrees / PerQuarterTurn);

        return true;
    }
}
