namespace Nodal.Simulation.Transient;

public sealed record TransientStatistics(
    int AcceptedSteps, int RejectedSteps, int NewtonIterations)
{
    public override string ToString() =>
        $"{AcceptedSteps} accepted, {RejectedSteps} rejected, {NewtonIterations} iterations";
}
