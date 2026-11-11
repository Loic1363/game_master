using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal static class ControlledFeedback
{
    public static SingularCause? Find(
        ImmutableArray<string> netNames, ImmutableArray<DcElement> elements)
    {
        var indexByNet = MnaPaths.IndexByNet(netNames);
        var joined = Conducting(elements, indexByNet, netNames.Length);

        var found = elements
            .Where(element => FeedsItsOwnControl(element, joined, indexByNet))
            .ToArray();

        if (found.Length == 0)
        {
            return null;
        }

        var nets = found
            .SelectMany(NetsOf)
            .Distinct(StringComparer.Ordinal)
            .Select(name => indexByNet[name])
            .Order()
            .Select(index => netNames[index])
            .ToImmutableArray();

        var verb = found.Length == 1 ? "forms" : "form";

        return new SingularCause(
            $"{NamedList.Of(found.Select(element => element.Designator))} {verb} a feedback loop "
            + "whose gain leaves the circuit undetermined.",
            [.. found.Select(element => element.Id)],
            nets);
    }

    private static DisjointSet Conducting(
        ImmutableArray<DcElement> elements, Dictionary<string, int> indexByNet, int netCount)
    {
        var joined = new DisjointSet(netCount);

        foreach (var element in elements.Where(candidate => candidate.Conducts))
        {
            foreach (var (first, second) in element.ConductionPaths)
            {
                joined.Union(indexByNet[first], indexByNet[second]);
            }
        }

        return joined;
    }

    private static bool FeedsItsOwnControl(
        DcElement element, DisjointSet joined, Dictionary<string, int> indexByNet)
    {
        if (element.Control is not { } control)
        {
            return false;
        }

        foreach (var output in new[] { element.FirstNode, element.SecondNode })
        {
            foreach (var sensed in new[] { control.PlusNode, control.MinusNode })
            {
                if (joined.AreJoined(indexByNet[output], indexByNet[sensed]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> NetsOf(DcElement element) =>
    [
        element.FirstNode,
        element.SecondNode,
        element.Control!.PlusNode,
        element.Control.MinusNode,
    ];
}
