using Nodal.Simulation.Dc;

namespace Nodal.Editor.Simulation;

public static class DcStatusText
{
    public const string NotRunText = "DC: not run";

    private const string SolvedPrefix = "DC: solved";
    private const string GminSteppingText = " by gmin stepping";
    private const string SourceSteppingText = " by source stepping";
    private const string IterationNoun = "iteration";

    public static string Of(OperatingPoint? operatingPoint)
    {
        if (operatingPoint?.Convergence is not { } convergence)
        {
            return NotRunText;
        }

        return SolvedPrefix
            + StrategyOf(convergence.Strategy)
            + $" ({Counted(convergence.Iterations)})";
    }

    private static string StrategyOf(DcSolveStrategy strategy) => strategy switch
    {
        DcSolveStrategy.GminStepping => GminSteppingText,
        DcSolveStrategy.SourceStepping => SourceSteppingText,
        _ => string.Empty,
    };

    private static string Counted(int iterations) =>
        iterations == 1 ? $"1 {IterationNoun}" : $"{iterations} {IterationNoun}s";
}
