namespace Nodal.Simulation.Dc;

internal static class DcStrategies
{
    private const double StartingNodeGmin = 1e-2;
    private const double GminDivisor = 10;
    private const double InitialSourceStep = 0.1;
    private const double MinimumSourceStep = 1.0 / 1024;
    private const double FullScale = 1;

    public static DcOutcome Run(NewtonSolver solver, DcOptions options, int elementCount)
    {
        var total = 0;
        var plain = solver.Iterate(new double[elementCount], 0, FullScale);

        total += plain.Iterations;

        if (plain.Converged || !options.StepOnFailure)
        {
            return new DcOutcome(plain, DcSolveStrategy.NewtonRaphson, total);
        }

        var stepped = GminStepping(solver, options, elementCount, ref total);

        if (stepped.Converged)
        {
            return new DcOutcome(stepped, DcSolveStrategy.GminStepping, total);
        }

        var ramped = SourceStepping(solver, elementCount, ref total);

        return new DcOutcome(
            ramped,
            ramped.Converged ? DcSolveStrategy.SourceStepping : DcSolveStrategy.NewtonRaphson,
            total);
    }

    private static DcAttempt GminStepping(
        NewtonSolver solver, DcOptions options, int elementCount, ref int total)
    {
        var junctions = new double[elementCount];
        var reached = DcAttempt.Failed(junctions, 0);

        foreach (var level in GminLevels(options))
        {
            var attempt = solver.Iterate(junctions, level, FullScale);

            total += attempt.Iterations;

            if (!attempt.Converged)
            {
                return DcAttempt.Failed(junctions, attempt.Iterations);
            }

            junctions = attempt.JunctionVoltages;
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

    private static DcAttempt SourceStepping(
        NewtonSolver solver, int elementCount, ref int total)
    {
        var junctions = new double[elementCount];
        var reached = solver.Iterate(junctions, 0, 0);

        total += reached.Iterations;

        if (!reached.Converged)
        {
            return reached;
        }

        junctions = reached.JunctionVoltages;

        var scale = 0.0;
        var step = InitialSourceStep;

        while (scale < FullScale)
        {
            var next = Math.Min(FullScale, scale + step);
            var attempt = solver.Iterate(junctions, 0, next);

            total += attempt.Iterations;

            if (attempt.Converged)
            {
                scale = next;
                junctions = attempt.JunctionVoltages;
                reached = attempt;

                continue;
            }

            step /= 2;

            if (step < MinimumSourceStep)
            {
                return DcAttempt.Failed(junctions, attempt.Iterations);
            }
        }

        return reached;
    }
}
