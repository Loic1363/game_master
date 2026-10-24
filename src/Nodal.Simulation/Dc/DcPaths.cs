using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Dc;

internal static class DcPaths
{
    public static ImmutableArray<string> NetsWithoutPathToGround(
        ImmutableArray<string> netNames, ImmutableArray<DcElement> elements)
    {
        var indexByNet = IndexByNet(netNames);
        var ground = indexByNet[SimulationDiagnostic.GroundNetName];
        var joined = new DisjointSet(netNames.Length);

        foreach (var element in elements.Where(candidate => candidate.Conducts))
        {
            joined.Union(indexByNet[element.FirstNet], indexByNet[element.SecondNet]);
        }

        return
        [
            .. netNames.Where(name => !joined.AreJoined(indexByNet[name], ground)),
        ];
    }

    public static Dictionary<string, int> IndexByNet(ImmutableArray<string> netNames)
    {
        var indexByNet = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var name in netNames)
        {
            indexByNet[name] = indexByNet.Count;
        }

        return indexByNet;
    }
}
