using System.Collections.Immutable;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal sealed class TransientStepper
{
    private const double AcceptedStepFraction = 0.9;
    private const double StepGrowthLimit = 2;
    private const double ProgressResolution = 0.01;
    private const int RejectionLimit = 64;

    private readonly ElaboratedCircuit _circuit;
    private readonly TransientOptions _options;
    private readonly TransientPlan _plan;
    private readonly TransientStorage _storage;
    private readonly TransientSystem _system;
    private readonly TransientNewton _newton;
    private readonly TransientBreakpoints _breakpoints;
    private readonly TransientRecorder _recorder;
    private readonly IProgress<double>? _progress;

    private double _reportedProgress;
    private int _accepted;
    private int _rejected;
    private int _iterations;

    private TransientStepper(
        ElaboratedCircuit circuit, TransientOptions options, IProgress<double>? progress)
    {
        _circuit = circuit;
        _options = options;
        _progress = progress;
        _plan = TransientPlan.Resolve(options);
        _storage = TransientStorage.Of(circuit);
        _system = new TransientSystem(circuit, _storage, options);
        _newton = new TransientNewton(circuit, _system, options);
        _breakpoints = TransientBreakpoints.Of(circuit, _plan);
        _recorder = new TransientRecorder(circuit, UndefinedNets(circuit, _system));
    }

    public static TransientResult Run(
        ElaboratedCircuit circuit,
        TransientOptions options,
        IProgress<double>? progress,
        CancellationToken cancellation)
    {
        var stepper = new TransientStepper(circuit, options, progress);

        return stepper.Run(cancellation);
    }

    private static ImmutableArray<string> UndefinedNets(
        ElaboratedCircuit circuit, TransientSystem system) =>
    [
        .. system.ShuntedNodes.Where(node => !circuit.IsInternal(node)),
    ];

    private TransientResult Run(CancellationToken cancellation)
    {
        if (!TransientStart.TrySolve(_circuit, _options, out var start, out var failure))
        {
            return TransientResult.Refused([failure]);
        }

        var state = InitialState(start);

        _recorder.Add(0, start.NodeVoltage, start.CurrentOf);

        var charge = new double[_storage.Count];
        var flow = new double[_storage.Count];

        Measure(state, charge, flow);

        var step = Math.Min(
            _plan.FirstStep, _plan.StepAfterBreakpoint(_breakpoints.Next(0)));

        var history = new TransientHistory(charge, flow, step);
        var order = TransientIntegration.BackwardEuler;
        var time = 0.0;
        var refused = 0;

        while (time < _plan.StopTime)
        {
            cancellation.ThrowIfCancellationRequested();

            var choice = Choose(step, time, _breakpoints.Next(time));

            step = choice.Step;

            if (step < _plan.MinStep || refused > RejectionLimit)
            {
                return Failed(time);
            }

            var integration = new TransientIntegration(order, step);
            var target = choice.Target;
            var solve = _newton.Solve(target, integration, state);

            _iterations += solve.Iterations;

            if (!solve.Converged)
            {
                _rejected++;
                refused++;
                step /= 2;
                order = TransientIntegration.BackwardEuler;

                if (step < _plan.MinStep)
                {
                    return Failed(time);
                }

                continue;
            }

            var previous = state;
            var solution = solve.Solution;
            var across = new double[_storage.Count];
            var through = new double[_storage.Count];

            for (var element = 0; element < _storage.Count; element++)
            {
                var index = _storage.PrimitiveOf(element);

                across[element] = _system.VoltageAcross(index, solution);
                through[element] = _system.CurrentOf(
                    index, solution, target, integration, previous);
            }

            var candidateCharge = new double[_storage.Count];
            var candidateFlow = new double[_storage.Count];

            for (var element = 0; element < _storage.Count; element++)
            {
                candidateCharge[element] = _storage.Charge(
                    element, across[element], through[element]);

                candidateFlow[element] = _storage.Flow(
                    element, across[element], through[element]);
            }

            var controlled = history.Real > order;

            var limit = controlled
                ? TruncationError.Limit(
                    _storage, history, candidateCharge, candidateFlow, integration, _options)
                : double.PositiveInfinity;

            var grown = Math.Min(StepGrowthLimit * step, limit);

            if (controlled && grown <= AcceptedStepFraction * step && step > _plan.MinStep)
            {
                _rejected++;
                refused++;
                step = Math.Max(grown, _plan.MinStep);

                continue;
            }

            _accepted++;
            refused = 0;
            time = target;
            history.Push(candidateCharge, candidateFlow, step);

            _recorder.Add(
                time,
                net => _system.Unknowns.NodeVoltage(net, solution),
                index => _system.CurrentOf(index, solution, target, integration, previous));

            state = new TransientState(across, through, solve.JunctionVoltages);

            Report(time);

            if (choice.Lands)
            {
                order = TransientIntegration.BackwardEuler;
                step = _plan.StepAfterBreakpoint(_breakpoints.Next(time) - time);
            }
            else
            {
                order = TransientIntegration.Trapezoidal;
                step = Math.Max(_plan.MinStep, Math.Min(grown, _plan.MaxStep));
            }
        }

        _progress?.Report(1);

        return _recorder.ToResult(
            [.. UndefinedNets(_circuit, _system).Select(SimulationDiagnostic.NoDcPath)],
            new TransientStatistics(_accepted, _rejected, _iterations));
    }

    private StepChoice Choose(double step, double time, double breakpoint)
    {
        var limited = Math.Min(step, _plan.MaxStep);

        if (time + limited >= breakpoint - _plan.MinBreak)
        {
            return new StepChoice(breakpoint - time, breakpoint, true);
        }

        if (time + (StepGrowthLimit * limited) > breakpoint)
        {
            limited = (breakpoint - time) / StepGrowthLimit;
        }

        return new StepChoice(limited, time + limited, false);
    }

    private TransientState InitialState(DcSolution start)
    {
        var across = new double[_storage.Count];
        var through = new double[_storage.Count];

        for (var element = 0; element < _storage.Count; element++)
        {
            var index = _storage.PrimitiveOf(element);

            across[element] = start.VoltageAcross(index);
            through[element] = start.CurrentOf(index);
        }

        var junctions = new double[_circuit.Primitives.Length];

        for (var index = 0; index < _circuit.Primitives.Length; index++)
        {
            if (_circuit.Primitives[index].Kind == PrimitiveKind.Diode)
            {
                junctions[index] = start.VoltageAcross(index);
            }
        }

        return new TransientState(across, through, junctions);
    }

    private void Measure(TransientState state, double[] charge, double[] flow)
    {
        for (var element = 0; element < _storage.Count; element++)
        {
            charge[element] = _storage.Charge(
                element, state.Across[element], state.Through[element]);

            flow[element] = _storage.Flow(
                element, state.Across[element], state.Through[element]);
        }
    }

    private void Report(double time)
    {
        if (_progress is null)
        {
            return;
        }

        var fraction = time / _plan.StopTime;

        if (fraction - _reportedProgress < ProgressResolution)
        {
            return;
        }

        _reportedProgress = fraction;
        _progress.Report(fraction);
    }

    private TransientResult Failed(double time) =>
        _recorder.ToPartial(
            [
                SimulationDiagnostic.TimestepTooSmall(time),
                .. UndefinedNets(_circuit, _system).Select(SimulationDiagnostic.NoDcPath),
            ],
            new TransientStatistics(_accepted, _rejected, _iterations),
            time);

    private readonly record struct StepChoice(double Step, double Target, bool Lands);
}
