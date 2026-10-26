using System.Collections.Immutable;
using Nodal.Simulation.LinearAlgebra;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class NewtonSolver
{
    private readonly MnaUnknowns _unknowns;
    private readonly ImmutableArray<DcElement> _elements;
    private readonly ImmutableArray<string> _netsWithoutDcPath;
    private readonly DcOptions _options;

    public NewtonSolver(
        MnaUnknowns unknowns,
        ImmutableArray<DcElement> elements,
        ImmutableArray<string> netsWithoutDcPath,
        DcOptions options)
    {
        _unknowns = unknowns;
        _elements = elements;
        _netsWithoutDcPath = netsWithoutDcPath;
        _options = options;
        HasNonlinearElements = elements.Any(element => element.IsNonlinear);
    }

    public bool HasNonlinearElements { get; }

    public DcAttempt SolveOnce()
    {
        var bias = DcBias.Rest(_elements.Length);

        return TrySolve(bias, out var solution)
            ? new DcAttempt(true, solution, bias.JunctionVoltages, 1)
            : DcAttempt.Failed(bias.JunctionVoltages, 1);
    }

    public DcAttempt Iterate(double[] startJunctionVoltages, double nodeGmin, double sourceScale)
    {
        var junctions = (double[])startJunctionVoltages.Clone();
        var previous = Array.Empty<double>();

        for (var iteration = 1; iteration <= _options.MaxIterations; iteration++)
        {
            var bias = DcBias.Rest(_elements.Length)
                .With(junctions)
                .At(nodeGmin, sourceScale);

            if (!TrySolve(bias, out var solution))
            {
                return DcAttempt.Failed(junctions, iteration);
            }

            var raw = JunctionVoltagesOf(solution);
            var limited = Limited(raw, junctions, out var limitedAny);

            if (previous.Length > 0
                && !limitedAny
                && NodesConverged(previous, solution)
                && CurrentsConverged(junctions, limited))
            {
                return new DcAttempt(true, solution, limited, iteration);
            }

            junctions = limited;
            previous = solution;
        }

        return DcAttempt.Failed(junctions, _options.MaxIterations);
    }

    private bool TrySolve(DcBias bias, out double[] solution)
    {
        var stamps = DcSystem.Build(_unknowns, _elements, _netsWithoutDcPath, _options, bias);

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
        var voltages = new double[_elements.Length];

        for (var index = 0; index < _elements.Length; index++)
        {
            if (_elements[index].IsNonlinear)
            {
                voltages[index] = VoltageAcross(_elements[index], solution);
            }
        }

        return voltages;
    }

    private double VoltageAcross(DcElement element, double[] solution) =>
        _unknowns.NodeVoltage(element.FirstNode, solution)
        - _unknowns.NodeVoltage(element.SecondNode, solution);

    private double[] Limited(double[] raw, double[] previous, out bool limitedAny)
    {
        var limited = new double[raw.Length];

        limitedAny = false;

        for (var index = 0; index < raw.Length; index++)
        {
            if (!_elements[index].IsNonlinear)
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
        for (var node = 0; node < _unknowns.NodeCount; node++)
        {
            if (!Close(previous[node], solution[node], _options.VnTol))
            {
                return false;
            }
        }

        return true;
    }

    private bool CurrentsConverged(double[] previous, double[] limited)
    {
        for (var index = 0; index < _elements.Length; index++)
        {
            if (!_elements[index].IsNonlinear)
            {
                continue;
            }

            if (!Close(
                    DiodeModel.Current(previous[index]),
                    DiodeModel.Current(limited[index]),
                    _options.AbsTol))
            {
                return false;
            }
        }

        return true;
    }

    private bool Close(double previous, double current, double absoluteTolerance) =>
        MnaConvergence.Close(previous, current, _options.RelTol, absoluteTolerance);
}
