using System.Collections.Immutable;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Mna;

internal sealed class MnaUnknowns
{
    private readonly Dictionary<string, int> _nodeByName = new(StringComparer.Ordinal);
    private readonly int[] _branchByElement;

    public MnaUnknowns(ImmutableArray<string> nodeNames, ImmutableArray<bool> elementsWithBranch)
    {
        foreach (var name in nodeNames)
        {
            if (!string.Equals(name, SimulationDiagnostic.GroundNetName, StringComparison.Ordinal))
            {
                _nodeByName[name] = _nodeByName.Count;
            }
        }

        NodeCount = _nodeByName.Count;
        _branchByElement = new int[elementsWithBranch.Length];
        Size = NodeCount;

        for (var index = 0; index < elementsWithBranch.Length; index++)
        {
            _branchByElement[index] = elementsWithBranch[index] ? Size++ : -1;
        }
    }

    public int NodeCount { get; }

    public int Size { get; }

    public int NodeOf(string nodeName) =>
        _nodeByName.TryGetValue(nodeName, out var index) ? index : -1;

    public int BranchOf(int elementIndex) => _branchByElement[elementIndex];

    public double NodeVoltage(string nodeName, double[] solution)
    {
        var index = NodeOf(nodeName);

        return index >= 0 ? solution[index] : 0;
    }
}
