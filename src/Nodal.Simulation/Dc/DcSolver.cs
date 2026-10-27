using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal static class DcSolver
{
    public static bool TrySolve(
        ElaboratedCircuit circuit,
        DcOptions options,
        [NotNullWhen(true)] out DcSolution? solution,
        [NotNullWhen(false)] out SimulationDiagnostic? failure)
    {
        var elements = DcElements.Resolve(circuit);
        var undefined = MnaPaths.NetsWithoutPathToGround(
            circuit.Nodes,
            elements.Where(element => element.Conducts).SelectMany(
                element => element.ConductionPaths));
        var unknowns = new MnaUnknowns(
            circuit.Nodes, [.. elements.Select(element => element.Branch is not null)]);
        var devices = NonlinearDevices.Resolve(circuit.Primitives, unknowns);
        var solver = new NewtonSolver(unknowns, elements, devices, undefined, options);
        var outcome = Run(solver, options);

        if (!outcome.Attempt.Converged)
        {
            solution = null;
            failure = Refusal(circuit, elements, solver.HasNonlinearElements, outcome);

            return false;
        }

        solution = new DcSolution(
            circuit,
            elements,
            unknowns,
            devices,
            undefined,
            outcome.Attempt.Solution,
            outcome.Attempt.DeviceState,
            new DcConvergence(outcome.Strategy, outcome.TotalIterations));

        failure = null;

        return true;
    }

    private static DcOutcome Run(NewtonSolver solver, DcOptions options)
    {
        if (solver.HasNonlinearElements)
        {
            return DcStrategies.Run(solver, options);
        }

        var direct = solver.SolveOnce();

        return new DcOutcome(direct, DcSolveStrategy.Direct, direct.Iterations);
    }

    private static SimulationDiagnostic Refusal(
        ElaboratedCircuit circuit,
        ImmutableArray<DcElement> elements,
        bool nonlinear,
        DcOutcome outcome)
    {
        if (nonlinear && DiodeAcrossIdealSource.Find(circuit.Nodes, elements) is { } unbounded)
        {
            return unbounded;
        }

        var cause = VoltageBranchCycles.Find(circuit.Nodes, elements);

        if (cause is null && nonlinear)
        {
            return SimulationDiagnostic.NonConvergence(outcome.TotalIterations);
        }

        return SimulationDiagnostic.Singular(
            cause?.Description, cause?.Components ?? [], cause?.Nets ?? []);
    }
}
