using Nodal.Simulation.Dc;
using Nodal.Simulation.LinearAlgebra;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Transient;

internal sealed class TransientNewton
{
    private readonly TransientSystem _system;
    private readonly NonlinearDevices _devices;
    private readonly TransientOptions _options;

    public TransientNewton(TransientSystem system, TransientOptions options)
    {
        _system = system;
        _devices = system.Devices;
        _options = options;
    }

    public TransientSolve Solve(
        double time, TransientIntegration integration, TransientState previous)
    {
        var state = (double[])previous.DeviceState.Clone();

        if (!_devices.Any)
        {
            return TrySolve(time, integration, previous, state, out var direct)
                ? new TransientSolve(true, direct, state, 1)
                : TransientSolve.Failed(1);
        }

        var iterate = Array.Empty<double>();

        for (var iteration = 1; iteration <= _options.MaxIterations; iteration++)
        {
            if (!TrySolve(time, integration, previous, state, out var solution))
            {
                return TransientSolve.Failed(iteration);
            }

            var limited = _devices.Limited(
                _devices.Measure(solution), state, out var limitedAny);

            if (iterate.Length > 0
                && !limitedAny
                && NodesConverged(iterate, solution)
                && CurrentsConverged(state, limited))
            {
                return new TransientSolve(true, solution, limited, iteration);
            }

            state = limited;
            iterate = solution;
        }

        return TransientSolve.Failed(_options.MaxIterations);
    }

    private bool TrySolve(
        double time,
        TransientIntegration integration,
        TransientState previous,
        double[] deviceState,
        out double[] solution)
    {
        var stamps = _system.Build(time, integration, previous, deviceState);

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
        var before = _devices.Currents(previous);
        var after = _devices.Currents(limited);

        for (var index = 0; index < before.Length; index++)
        {
            if (!MnaConvergence.Close(
                    before[index], after[index], _options.RelTol, _options.AbsTol))
            {
                return false;
            }
        }

        return true;
    }
}
