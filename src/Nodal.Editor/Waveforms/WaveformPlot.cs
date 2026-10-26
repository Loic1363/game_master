using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Waveforms;

public sealed class WaveformPlot
{
    private WaveformPlot(
        TransientResult? result,
        ImmutableArray<WaveformTrace> traces,
        bool isStale,
        AxisRange timeLimits,
        AxisRange voltageLimits,
        AxisRange currentLimits)
    {
        Result = result;
        Traces = traces;
        IsStale = isStale;
        TimeLimits = timeLimits;
        VoltageLimits = voltageLimits;
        CurrentLimits = currentLimits;
    }

    public static WaveformPlot None { get; } = new(
        null, [], false, AxisRange.Around(0), AxisRange.Around(0), AxisRange.Around(0));

    public TransientResult? Result { get; }

    public ImmutableArray<WaveformTrace> Traces { get; }

    public bool IsStale { get; }

    public AxisRange TimeLimits { get; }

    public AxisRange VoltageLimits { get; }

    public AxisRange CurrentLimits { get; }

    public bool HasResult => Result is { HasValues: true };

    public bool HasTraces => !Traces.IsEmpty;

    public bool HasVoltage => Traces.Any(trace => trace.Axis == TraceAxis.Volts);

    public bool HasCurrent => Traces.Any(trace => trace.Axis == TraceAxis.Amperes);

    public bool IsPartial => Result is { IsPartial: true };

    public double? FailureTime => Result?.FailureTime;

    public string? StatusText => IsStale
        ? TransientFailureText.StaleText
        : Result is { } result ? TransientFailureText.Of(result) : null;

    public static WaveformPlot Of(
        TransientResult result,
        ConnectivityResult connectivity,
        ImmutableArray<Probe> probes,
        bool isStale = false)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(connectivity);

        var traces = ProbeTraces.Of(connectivity, result, probes);

        return new WaveformPlot(
            result,
            traces,
            isStale,
            result.HasValues
                ? new AxisRange(result.Times[0], result.FinalTime)
                : AxisRange.Around(0),
            FittedOf(traces, TraceAxis.Volts),
            FittedOf(traces, TraceAxis.Amperes));
    }

    public WaveformPlot Staled() => IsStale || Result is null
        ? this
        : new WaveformPlot(
            Result, Traces, true, TimeLimits, VoltageLimits, CurrentLimits);

    public AxisRange LimitsOf(TraceAxis axis) =>
        axis == TraceAxis.Volts ? VoltageLimits : CurrentLimits;

    private static AxisRange FittedOf(ImmutableArray<WaveformTrace> traces, TraceAxis axis) =>
        AxisRange.Fitting(
            traces.Where(trace => trace.Axis == axis).SelectMany(trace => trace.Values));
}
