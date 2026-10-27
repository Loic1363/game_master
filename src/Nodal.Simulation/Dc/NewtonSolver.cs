using System.Collections.Immutable;
using Nodal.Simulation.LinearAlgebra;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class NewtonSolver
{
    private readonly MnaUnknowns _unknowns;
    private readonly ImmutableArray<DcElement> _elements;
    private readonly NonlinearDevices _devices;
    private readonly ImmutableArray<string> _netsWithoutDcPath;
    private readonly DcOptions _options;

    public NewtonSolver(
        MnaUnknowns unknowns,
        ImmutableArray<DcElement> elements,
        NonlinearDevices devices,
        ImmutableArray<string> netsWithoutDcPath,
        DcOptions options)
    {
        _unknowns = unknowns;
        _elements = elements;
        _devices = devices;
        _netsWithoutDcPath = netsWithoutDcPath;
        _options = options;
    }

    public bool HasNonlinearElements => _devices.Any;

    public int StateCount => _devices.StateCount;

    public DcAttempt SolveOnce()
    {
        var bias = DcBias.Rest(_devices.StateCount);

        return TrySolve(bias, out var solution)
            ? new DcAttempt(true, solution, bias.DeviceState, 1)
            : DcAttempt.Failed(bias.DeviceState, 1);
    }

    public DcAttempt Iterate(double[] startState, double nodeGmin, double sourceScale)
    {
        var state = (double[])startState.Clone();
        var previous = Array.Empty<double>();

        for (var iteration = 1; iteration <= _options.MaxIterations; iteration++)
        {
            var bias = DcBias.Rest(_devices.StateCount)
                .With(state)
                .At(nodeGmin, sourceScale);

            if (!TrySolve(bias, out var solution))
            {
                return DcAttempt.Failed(state, iteration);
            }

            var limited = _devices.Limited(
                _devices.Measure(solution), state, out var limitedAny);

            if (previous.Length > 0
                && !limitedAny
                && NodesConverged(previous, solution)
                && CurrentsConverged(state, limited))
            {
                return new DcAttempt(true, solution, limited, iteration);
            }

            state = limited;
            previous = solution;
        }

        return DcAttempt.Failed(state, _options.MaxIterations);
    }

    private bool TrySolve(DcBias bias, out double[] solution)
    {
        var stamps = DcSystem.Build(
            _unknowns, _elements, _devices, _netsWithoutDcPath, _options, bias);

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
        var before = _devices.Currents(previous);
        var after = _devices.Currents(limited);

        for (var index = 0; index < before.Length; index++)
        {
            if (!Close(before[index], after[index], _options.AbsTol))
            {
                return false;
            }
        }

        return true;
    }

    private bool Close(double previous, double current, double absoluteTolerance) =>
        MnaConvergence.Close(previous, current, _options.RelTol, absoluteTolerance);
}
