using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal static class VoltageBranchCycles
{
    public static SingularCause? Find(
        ImmutableArray<string> netNames, ImmutableArray<DcElement> elements)
    {
        var indexByNet = MnaPaths.IndexByNet(netNames);
        var joined = new DisjointSet(netNames.Length);
        var accepted = new List<BranchEdge>();

        foreach (var element in elements.Where(candidate => candidate.HasIdealBranch))
        {
            var edge = new BranchEdge(
                element, indexByNet[element.FirstNode], indexByNet[element.SecondNode]);

            if (edge.First == edge.Second)
            {
                return ShortCircuit(edge, netNames);
            }

            if (joined.AreJoined(edge.First, edge.Second))
            {
                return Cycle([.. PathBetween(accepted, edge.First, edge.Second), edge], netNames);
            }

            joined.Union(edge.First, edge.Second);
            accepted.Add(edge);
        }

        return null;
    }

    private static SingularCause ShortCircuit(BranchEdge edge, ImmutableArray<string> netNames) =>
        new($"{edge.Element.Designator} has both terminals on net {netNames[edge.First]}, "
            + "which short-circuits it.",
            [edge.Element.Id],
            [netNames[edge.First]]);

    private static SingularCause Cycle(
        ImmutableArray<BranchEdge> found, ImmutableArray<string> netNames)
    {
        var cycle = found.Sort((left, right) => left.Element.Id.CompareTo(right.Element.Id));
        var designators = Listed(cycle.Select(edge => edge.Element.Designator));
        var noun = NounFor(cycle);
        var nets = cycle
            .SelectMany(edge => new[] { edge.First, edge.Second })
            .Distinct()
            .Order()
            .Select(index => netNames[index])
            .ToImmutableArray();

        var description = IsParallel(cycle)
            ? $"{designators} are {noun} in parallel across nets {Listed(nets)}."
            : $"{designators} form a loop of {noun}.";

        return new SingularCause(
            description, [.. cycle.Select(edge => edge.Element.Id)], nets);
    }

    private static bool IsParallel(ImmutableArray<BranchEdge> cycle) =>
        cycle.All(edge =>
            Math.Min(edge.First, edge.Second) == Math.Min(cycle[0].First, cycle[0].Second)
            && Math.Max(edge.First, edge.Second) == Math.Max(cycle[0].First, cycle[0].Second));

    private static string NounFor(ImmutableArray<BranchEdge> cycle)
    {
        var kinds = cycle.Select(edge => edge.Element.Kind).Distinct().ToArray();

        if (kinds.Length > 1)
        {
            return "zero-resistance branches";
        }

        return kinds[0] switch
        {
            PrimitiveKind.VoltageSource => "ideal voltage sources",
            PrimitiveKind.Inductor => "ideal inductors",
            _ => "zero-resistance branches",
        };
    }

    private static List<BranchEdge> PathBetween(List<BranchEdge> edges, int from, int to)
    {
        var cameFrom = new Dictionary<int, BranchEdge>();
        var visited = new HashSet<int> { from };
        var queue = new Queue<int>();

        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var net = queue.Dequeue();

            if (net == to)
            {
                return Retrace(cameFrom, from, to);
            }

            foreach (var edge in edges.Where(candidate => candidate.Touches(net)))
            {
                var next = edge.Other(net);

                if (visited.Add(next))
                {
                    cameFrom[next] = edge;
                    queue.Enqueue(next);
                }
            }
        }

        return [];
    }

    private static List<BranchEdge> Retrace(
        Dictionary<int, BranchEdge> cameFrom, int from, int to)
    {
        var path = new List<BranchEdge>();
        var net = to;

        while (net != from && cameFrom.TryGetValue(net, out var edge))
        {
            path.Add(edge);
            net = edge.Other(net);
        }

        path.Reverse();

        return path;
    }

    private static string Listed(IEnumerable<string> names)
    {
        var all = names.ToArray();

        return all.Length <= 1
            ? string.Concat(all)
            : string.Join(", ", all[..^1]) + " and " + all[^1];
    }

    private sealed record BranchEdge(DcElement Element, int First, int Second)
    {
        public bool Touches(int net) => First == net || Second == net;

        public int Other(int net) => net == First ? Second : First;
    }
}
