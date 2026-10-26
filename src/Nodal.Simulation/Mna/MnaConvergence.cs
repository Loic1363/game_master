namespace Nodal.Simulation.Mna;

internal static class MnaConvergence
{
    public static bool Close(
        double previous, double current, double relativeTolerance, double absoluteTolerance) =>
        Math.Abs(current - previous)
        <= (relativeTolerance * Math.Max(Math.Abs(current), Math.Abs(previous)))
            + absoluteTolerance;
}
