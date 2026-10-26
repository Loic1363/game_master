using Nodal.Content.Components;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientThreadingTests
{
    private const double Load = 1000;
    private const double Capacitance = 100e-9;

    private sealed class Recorder : IProgress<double>
    {
        public List<double> Values { get; } = [];

        public void Report(double value) => Values.Add(value);
    }

    private sealed class Canceller : IProgress<double>
    {
        private readonly CancellationTokenSource _source;

        public Canceller(CancellationTokenSource source) => _source = source;

        public int Reports { get; private set; }

        public void Report(double value)
        {
            Reports++;
            _source.Cancel();
        }
    }

    private static TransientResult Run(
        IProgress<double>? progress, CancellationToken cancellation) =>
        TransientAnalysis.Run(
            TransientCircuits.Rc(Load, Capacitance).Set("V1", "voltage", 5).Compile(),
            BaseCatalog.Catalog,
            new TransientOptions(1e-3) { StartFromZero = true },
            progress,
            cancellation);

    [Fact]
    public void ProgressRisesFromZeroToOne()
    {
        var recorder = new Recorder();
        var result = Run(recorder, CancellationToken.None);

        Assert.True(result.IsSolved);
        Assert.NotEmpty(recorder.Values);
        Assert.Equal(1, recorder.Values[^1]);

        for (var index = 1; index < recorder.Values.Count; index++)
        {
            Assert.True(recorder.Values[index] >= recorder.Values[index - 1]);
        }

        Assert.All(recorder.Values, value => Assert.InRange(value, 0, 1));
    }

    [Fact]
    public void ProgressIsReportedAtMostOncePerPercent()
    {
        var recorder = new Recorder();

        Run(recorder, CancellationToken.None);

        Assert.True(recorder.Values.Count <= 101);
    }

    [Fact]
    public void CancellationStopsTheRun()
    {
        using var source = new CancellationTokenSource();
        var canceller = new Canceller(source);

        Assert.Throws<OperationCanceledException>(() => Run(canceller, source.Token));
        Assert.Equal(1, canceller.Reports);
    }

    [Fact]
    public void ACancelledTokenStopsTheRunBeforeTheFirstStep()
    {
        using var source = new CancellationTokenSource();

        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => Run(null, source.Token));
    }

    [Fact]
    public void TheAnalysisIsAPureFunctionOfItsInputs()
    {
        var schematic = TransientCircuits.Rc(Load, Capacitance).Set("V1", "voltage", 5);
        var options = new TransientOptions(1e-3) { StartFromZero = true };

        var first = schematic.Transient(options);
        var second = schematic.Transient(options);

        Assert.Equal(first.PointCount, second.PointCount);
        Assert.Equal(first.Times, second.Times);
        Assert.Equal(first.Of("C1").Voltages, second.Of("C1").Voltages);
        Assert.Equal(first.Statistics, second.Statistics);
    }

    [Fact]
    public async Task TheAnalysisRunsOnABackgroundTask()
    {
        var schematic = TransientCircuits.Rc(Load, Capacitance).Set("V1", "voltage", 5);
        var netlist = schematic.Compile();

        var result = await Task.Run(
            () => TransientAnalysis.Run(
                netlist,
                BaseCatalog.Catalog,
                new TransientOptions(1e-3) { StartFromZero = true }));

        Assert.True(result.IsSolved);
        Approximately.Equal(5, result.VoltsAt("C1", 1e-3), 1e-3);
    }
}
