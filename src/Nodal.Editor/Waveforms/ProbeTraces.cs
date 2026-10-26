using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Waveforms;

public static class ProbeTraces
{
    public static ImmutableArray<WaveformTrace> Of(
        ConnectivityResult connectivity, TransientResult result, ImmutableArray<Probe> probes)
    {
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(result);

        if (probes.IsDefaultOrEmpty || !result.HasValues)
        {
            return [];
        }

        var traces = ImmutableArray.CreateBuilder<WaveformTrace>(probes.Length);

        for (var index = 0; index < probes.Length; index++)
        {
            if (TraceOf(connectivity, result, probes[index], index) is { } trace)
            {
                traces.Add(trace);
            }
        }

        return traces.ToImmutable();
    }

    public static string? NetOf(ConnectivityResult connectivity, Probe probe)
    {
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(probe);

        return probe switch
        {
            PinVoltageProbe pin =>
                connectivity.TryGetNetOfPin(pin.Pin, out var pinNet) ? pinNet.Name : null,
            WireVoltageProbe wire =>
                connectivity.TryGetNetOfWire(wire.Wire, out var wireNet) ? wireNet.Name : null,
            _ => null,
        };
    }

    private static WaveformTrace? TraceOf(
        ConnectivityResult connectivity, TransientResult result, Probe probe, int index) =>
        probe is ComponentCurrentProbe current
            ? CurrentTrace(result, current, index)
            : VoltageTrace(connectivity, result, probe, index);

    private static WaveformTrace? VoltageTrace(
        ConnectivityResult connectivity, TransientResult result, Probe probe, int index)
    {
        if (NetOf(connectivity, probe) is not { } netName
            || !result.TryGetNetVoltages(netName, out var voltages)
            || voltages.IsDefaultOrEmpty)
        {
            return null;
        }

        return new WaveformTrace(
            probe, index, $"V({netName})", TraceAxis.Volts, voltages);
    }

    private static WaveformTrace? CurrentTrace(
        TransientResult result, ComponentCurrentProbe probe, int index)
    {
        if (!result.TryGetComponent(probe.Component, out var series)
            || series.Currents.IsDefaultOrEmpty)
        {
            return null;
        }

        return new WaveformTrace(
            probe, index, $"I({series.Designator})", TraceAxis.Amperes, series.Currents);
    }
}
