using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Mna;

internal static class MnaPaths
{
    public static ImmutableArray<string> NetsWithoutPathToGround(
        ImmutableArray<string> netNames, IEnumerable<(string First, string Second)> conducting)
    {
        var indexByNet = IndexByNet(netNames);
        var ground = indexByNet[SimulationDiagnostic.GroundNetName];
        var joined = new DisjointSet(netNames.Length);

        foreach (var (first, second) in conducting)
        {
            joined.Union(indexByNet[first], indexByNet[second]);
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
