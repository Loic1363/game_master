namespace Nodal.Simulation.Dc;

internal static class DcStrategies
{
    private const double StartingNodeGmin = 1e-2;
    private const double GminDivisor = 10;
    private const double InitialSourceStep = 0.1;
    private const double MinimumSourceStep = 1.0 / 1024;
    private const double FullScale = 1;

    public static DcOutcome Run(NewtonSolver solver, DcOptions options)
    {
        var total = 0;
        var plain = solver.Iterate(new double[solver.StateCount], 0, FullScale);

        total += plain.Iterations;

        if (plain.Converged || !options.StepOnFailure)
        {
            return new DcOutcome(plain, DcSolveStrategy.NewtonRaphson, total);
        }

        var stepped = GminStepping(solver, options, ref total);

        if (stepped.Converged)
        {
            return new DcOutcome(stepped, DcSolveStrategy.GminStepping, total);
        }

        var ramped = SourceStepping(solver, ref total);

        return new DcOutcome(
            ramped,
            ramped.Converged ? DcSolveStrategy.SourceStepping : DcSolveStrategy.NewtonRaphson,
            total);
    }

    private static DcAttempt GminStepping(
        NewtonSolver solver, DcOptions options, ref int total)
    {
        var state = new double[solver.StateCount];
        var reached = DcAttempt.Failed(state, 0);

        foreach (var level in GminLevels(options))
        {
            var attempt = solver.Iterate(state, level, FullScale);

            total += attempt.Iterations;

            if (!attempt.Converged)
            {
                return DcAttempt.Failed(state, attempt.Iterations);
            }

            state = attempt.DeviceState;
            reached = attempt;
        }

        return reached;
    }

    private static IEnumerable<double> GminLevels(DcOptions options)
    {
        for (var level = StartingNodeGmin; level > options.Gmin; level /= GminDivisor)
        {
            yield return level;
        }

        yield return 0;
    }

    private static DcAttempt SourceStepping(NewtonSolver solver, ref int total)
    {
        var state = new double[solver.StateCount];
        var reached = solver.Iterate(state, 0, 0);

        total += reached.Iterations;

        if (!reached.Converged)
        {
            return reached;
        }

        state = reached.DeviceState;

        var scale = 0.0;
        var step = InitialSourceStep;

        while (scale < FullScale)
        {
            var next = Math.Min(FullScale, scale + step);
            var attempt = solver.Iterate(state, 0, next);

            total += attempt.Iterations;

            if (attempt.Converged)
            {
                scale = next;
                state = attempt.DeviceState;
                reached = attempt;

                continue;
            }

            step /= 2;

            if (step < MinimumSourceStep)
            {
                return DcAttempt.Failed(state, attempt.Iterations);
            }
        }

        return reached;
    }
}
