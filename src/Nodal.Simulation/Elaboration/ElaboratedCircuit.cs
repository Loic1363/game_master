using System.Collections.Immutable;
using System.Text;

namespace Nodal.Simulation.Elaboration;

public sealed class ElaboratedCircuit
{
    private readonly HashSet<string> _externalNets;

    internal ElaboratedCircuit(
        ImmutableArray<Primitive> primitives,
        ImmutableArray<ElaboratedComponent> components,
        ImmutableArray<string> nodes,
        ImmutableArray<string> externalNets)
    {
        Primitives = primitives;
        Components = components;
        Nodes = nodes;
        ExternalNets = externalNets;
        _externalNets = [.. externalNets];
    }

    public ImmutableArray<Primitive> Primitives { get; }

    public ImmutableArray<ElaboratedComponent> Components { get; }

    public ImmutableArray<string> Nodes { get; }

    public ImmutableArray<string> ExternalNets { get; }

    public ImmutableArray<string> InternalNodes =>
        [.. Nodes.Where(node => !_externalNets.Contains(node))];

    public bool IsInternal(string node) => !_externalNets.Contains(node);

    public string ToDebugText()
    {
        var text = new StringBuilder();

        foreach (var primitive in Primitives)
        {
            if (text.Length > 0)
            {
                text.Append('\n');
            }

            text.Append(primitive);
        }

        return text.ToString();
    }
}
