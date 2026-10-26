using System.Collections.Immutable;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.LinearAlgebra;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Transient;

internal sealed class TransientNewton
{
    private readonly TransientSystem _system;
    private readonly ImmutableArray<Primitive> _primitives;
    private readonly TransientOptions _options;
    private readonly bool _nonlinear;

    public TransientNewton(
        ElaboratedCircuit circuit, TransientSystem system, TransientOptions options)
    {
        _system = system;
        _primitives = circuit.Primitives;
        _options = options;
        _nonlinear = circuit.Primitives.Any(primitive => primitive.Kind == PrimitiveKind.Diode);
    }

    public TransientSolve Solve(
        double time, TransientIntegration integration, TransientState previous)
    {
        var junctions = (double[])previous.JunctionVoltages.Clone();

        if (!_nonlinear)
        {
            return TrySolve(time, integration, previous, junctions, out var direct)
                ? new TransientSolve(true, direct, junctions, 1)
                : TransientSolve.Failed(1);
        }

        var iterate = Array.Empty<double>();

        for (var iteration = 1; iteration <= _options.MaxIterations; iteration++)
        {
            if (!TrySolve(time, integration, previous, junctions, out var solution))
            {
                return TransientSolve.Failed(iteration);
            }

            var limited = Limited(JunctionVoltagesOf(solution), junctions, out var limitedAny);

            if (iterate.Length > 0
                && !limitedAny
                && NodesConverged(iterate, solution)
                && CurrentsConverged(junctions, limited))
            {
                return new TransientSolve(true, solution, limited, iteration);
            }

            junctions = limited;
            iterate = solution;
        }

        return TransientSolve.Failed(_options.MaxIterations);
    }

    private bool TrySolve(
        double time,
        TransientIntegration integration,
        TransientState previous,
        double[] junctions,
        out double[] solution)
    {
        var stamps = _system.Build(time, integration, previous, junctions);

        if (!LuDecomposition.TryFactor(stamps.Matrix, out var decomposition))
        {
            solution = [];

            return false;
        }

        solution = decomposition.Solve(stamps.RightHandSide);

        if (solution.All(double.IsFinite))
        {
            return true;
        }

        solution = [];

        return false;
    }

    private double[] JunctionVoltagesOf(double[] solution)
    {
        var voltages = new double[_primitives.Length];

        for (var index = 0; index < _primitives.Length; index++)
        {
            if (_primitives[index].Kind == PrimitiveKind.Diode)
            {
                voltages[index] = _system.VoltageAcross(index, solution);
            }
        }

        return voltages;
    }

    private double[] Limited(double[] raw, double[] previous, out bool limitedAny)
    {
        var limited = new double[raw.Length];

        limitedAny = false;

        for (var index = 0; index < raw.Length; index++)
        {
            if (_primitives[index].Kind != PrimitiveKind.Diode)
            {
                continue;
            }

            limited[index] = DiodeModel.Limit(raw[index], previous[index], out var wasLimited);
            limitedAny |= wasLimited;
        }

        return limited;
    }

    private bool NodesConverged(double[] previous, double[] solution)
    {
        for (var node = 0; node < _system.Unknowns.NodeCount; node++)
        {
            if (!MnaConvergence.Close(
                    previous[node], solution[node], _options.RelTol, _options.VnTol))
            {
                return false;
            }
        }

        return true;
    }

    private bool CurrentsConverged(double[] previous, double[] limited)
    {
        for (var index = 0; index < _primitives.Length; index++)
        {
            if (_primitives[index].Kind != PrimitiveKind.Diode)
            {
                continue;
            }

            if (!MnaConvergence.Close(
                    DiodeModel.Current(previous[index]),
                    DiodeModel.Current(limited[index]),
                    _options.RelTol,
                    _options.AbsTol))
            {
                return false;
            }
        }

        return true;
    }
}
