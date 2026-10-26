using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;

namespace Nodal.Editor.Waveforms;

public sealed class WaveformTrace
{
    internal WaveformTrace(
        Probe probe, int colorIndex, string label, TraceAxis axis, ImmutableArray<double> values)
    {
        Probe = probe;
        ColorIndex = colorIndex;
        Label = label;
        Axis = axis;
        Values = values;
    }

    public Probe Probe { get; }

    public int ColorIndex { get; }

    public string Label { get; }

    public TraceAxis Axis { get; }

    public ImmutableArray<double> Values { get; }

    public Unit Unit => Axis == TraceAxis.Volts ? Unit.Volt : Unit.Ampere;

    public override string ToString() => Label;
}
