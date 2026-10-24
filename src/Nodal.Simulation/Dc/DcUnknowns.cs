using System.Collections.Immutable;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Dc;

internal sealed class DcUnknowns
{
    private readonly Dictionary<string, int> _nodeByNet = new(StringComparer.Ordinal);
    private readonly int[] _branchByElement;

    public DcUnknowns(ImmutableArray<string> netNames, ImmutableArray<DcElement> elements)
    {
        foreach (var name in netNames)
        {
            if (!string.Equals(name, SimulationDiagnostic.GroundNetName, StringComparison.Ordinal))
            {
                _nodeByNet[name] = _nodeByNet.Count;
            }
        }

        NodeCount = _nodeByNet.Count;
        _branchByElement = new int[elements.Length];
        Size = NodeCount;

        for (var index = 0; index < elements.Length; index++)
        {
            _branchByElement[index] = elements[index].Branch is null ? -1 : Size++;
        }
    }

    public int NodeCount { get; }

    public int Size { get; }

    public int NodeOf(string netName) =>
        _nodeByNet.TryGetValue(netName, out var index) ? index : -1;

    public int BranchOf(int elementIndex) => _branchByElement[elementIndex];
}
