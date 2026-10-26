namespace Nodal.Simulation.Dc;

internal sealed record DcOutcome(DcAttempt Attempt, DcSolveStrategy Strategy, int TotalIterations);
