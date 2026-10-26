using Nodal.Circuit.Commands;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;

namespace Nodal.Editor.Waveforms;

public static class ProbeToggle
{
    public static IEditCommand Of(
        Schematic schematic, ConnectivityResult connectivity, Probe probe)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(probe);

        return Existing(schematic, connectivity, probe) is { } existing
            ? new RemoveProbe(existing)
            : new AddProbe(probe);
    }

    public static Probe? Existing(
        Schematic schematic, ConnectivityResult connectivity, Probe probe)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(probe);

        foreach (var candidate in schematic.Analysis.Probes)
        {
            if (AreTheSameTarget(connectivity, candidate, probe))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool AreTheSameTarget(
        ConnectivityResult connectivity, Probe candidate, Probe probe)
    {
        if (candidate == probe)
        {
            return true;
        }

        if (candidate.IsCurrent || probe.IsCurrent)
        {
            return false;
        }

        return ProbeTraces.NetOf(connectivity, candidate) is { } candidateNet
            && ProbeTraces.NetOf(connectivity, probe) is { } probeNet
            && string.Equals(candidateNet, probeNet, StringComparison.Ordinal);
    }
}
