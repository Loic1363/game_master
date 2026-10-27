using System.Collections.Immutable;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal static class DcSystem
{
    public static MnaStamps Build(
        MnaUnknowns unknowns,
        ImmutableArray<DcElement> elements,
        NonlinearDevices devices,
        ImmutableArray<string> netsWithoutDcPath,
        DcOptions options,
        DcBias bias)
    {
        var stamps = new MnaStamps(unknowns.Size);

        for (var index = 0; index < elements.Length; index++)
        {
            Stamp(stamps, unknowns, elements[index], unknowns.BranchOf(index), bias);
        }

        devices.Stamp(stamps, bias.DeviceState);

        foreach (var name in netsWithoutDcPath)
        {
            stamps.Shunt(unknowns.NodeOf(name), options.Gmin);
        }

        for (var node = 0; node < unknowns.NodeCount; node++)
        {
            stamps.Shunt(node, bias.NodeGmin);
        }

        return stamps;
    }

    private static void Stamp(
        MnaStamps stamps, MnaUnknowns unknowns, DcElement element, int branch, DcBias bias)
    {
        var plus = unknowns.NodeOf(element.FirstNode);
        var minus = unknowns.NodeOf(element.SecondNode);

        if (element.Conductance is { } conductance)
        {
            stamps.Conductance(plus, minus, conductance);
        }

        if (element.Branch is { } definition)
        {
            stamps.Branch(
                branch,
                plus,
                minus,
                definition.Resistance,
                definition.Voltage * bias.SourceScale);
        }

        if (element.SourceCurrent != 0)
        {
            stamps.Current(plus, minus, element.SourceCurrent * bias.SourceScale);
        }
    }
}
