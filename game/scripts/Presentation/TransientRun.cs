using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Simulation.Transient;

namespace Nodal.Game.Presentation;

public sealed class TransientRun
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Progress _progress = new();
    private readonly Task<TransientResult?> _task;

    private TransientRun(
        Schematic schematic, Netlist netlist, ComponentCatalog catalog, TransientOptions options)
    {
        Schematic = schematic;
        _task = Task.Run(() => Solve(netlist, catalog, options), _cancellation.Token);
    }

    public Schematic Schematic { get; }

    public bool IsRunning => !_task.IsCompleted;

    public double Fraction => _progress.Fraction;

    public bool WasCancelled => _cancellation.IsCancellationRequested;

    public static TransientRun Start(
        Schematic schematic, ComponentCatalog catalog, TransientOptions options)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(options);

        return new TransientRun(
            schematic, NetlistCompiler.Compile(schematic, catalog), catalog, options);
    }

    public void Cancel() => _cancellation.Cancel();

    public bool TryTakeResult(out TransientResult? result)
    {
        result = null;

        if (!_task.IsCompleted)
        {
            return false;
        }

        result = _task.IsCompletedSuccessfully ? _task.Result : null;

        return true;
    }

    private TransientResult? Solve(
        Netlist netlist, ComponentCatalog catalog, TransientOptions options)
    {
        try
        {
            return TransientAnalysis.Run(
                netlist, catalog, options, _progress, _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private sealed class Progress : IProgress<double>
    {
        private double _fraction;

        public double Fraction => Volatile.Read(ref _fraction);

        public void Report(double value) => Volatile.Write(ref _fraction, value);
    }
}
