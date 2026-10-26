using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal static class DiodeAcrossIdealSource
{
    public static SimulationDiagnostic? Find(
        ImmutableArray<string> netNames, ImmutableArray<DcElement> elements)
    {
        var indexByNet = MnaPaths.IndexByNet(netNames);
        var joined = Shorts(netNames, elements, indexByNet);

        foreach (var source in elements.Where(IsDrivingIdealSource))
        {
            var plus = joined.Find(indexByNet[source.FirstNode]);
            var minus = joined.Find(indexByNet[source.SecondNode]);

            if (plus == minus)
            {
                continue;
            }

            var forward = source.Branch!.Voltage > 0 ? plus : minus;
            var reverse = source.Branch!.Voltage > 0 ? minus : plus;

            foreach (var diode in elements.Where(candidate => candidate.IsNonlinear))
            {
                if (joined.Find(indexByNet[diode.FirstNode]) == forward
                    && joined.Find(indexByNet[diode.SecondNode]) == reverse)
                {
                    return SimulationDiagnostic.UnboundedCurrent(
                        diode.Id,
                        diode.Designator,
                        source.Id,
                        source.Designator,
                        [diode.FirstNode, diode.SecondNode]);
                }
            }
        }

        return null;
    }

    private static DisjointSet Shorts(
        ImmutableArray<string> netNames,
        ImmutableArray<DcElement> elements,
        Dictionary<string, int> indexByNet)
    {
        var joined = new DisjointSet(netNames.Length);

        foreach (var element in elements.Where(IsShort))
        {
            joined.Union(indexByNet[element.FirstNode], indexByNet[element.SecondNode]);
        }

        return joined;
    }

    private static bool IsShort(DcElement element) =>
        element.Branch is { IsIdeal: true, Voltage: 0 };

    private static bool IsDrivingIdealSource(DcElement element) =>
        element.Kind == PrimitiveKind.VoltageSource
        && element.Branch is { IsIdeal: true } branch
        && branch.Voltage != 0;
}
