using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class DcSolution
{
    private readonly double[] _values;

    public DcSolution(
        ElaboratedCircuit circuit,
        ImmutableArray<DcElement> elements,
        MnaUnknowns unknowns,
        ImmutableArray<string> undefinedNodes,
        double[] values,
        DcConvergence convergence)
    {
        Circuit = circuit;
        Elements = elements;
        Unknowns = unknowns;
        UndefinedNodes = undefinedNodes;
        Convergence = convergence;
        _values = values;
    }

    public ElaboratedCircuit Circuit { get; }

    public ImmutableArray<DcElement> Elements { get; }

    public MnaUnknowns Unknowns { get; }

    public ImmutableArray<string> UndefinedNodes { get; }

    public DcConvergence Convergence { get; }

    public double NodeVoltage(string node)
    {
        var index = Unknowns.NodeOf(node);

        return index >= 0 ? _values[index] : 0;
    }

    public double VoltageAcross(int primitive) =>
        NodeVoltage(Elements[primitive].FirstNode)
        - NodeVoltage(Elements[primitive].SecondNode);

    public double CurrentOf(int primitive)
    {
        var element = Elements[primitive];
        var across = VoltageAcross(primitive);

        if (element.IsNonlinear)
        {
            return DiodeModel.Current(across);
        }

        var branch = Unknowns.BranchOf(primitive);

        return (branch >= 0 ? _values[branch] : across * (element.Conductance ?? 0))
            - element.SourceCurrent;
    }
}
