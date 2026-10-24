using System.Collections.Immutable;
using Nodal.Simulation.LinearAlgebra;

namespace Nodal.Simulation.Dc;

internal sealed class DcSystem
{
    private DcSystem(DenseMatrix matrix, double[] rightHandSide, DcUnknowns unknowns)
    {
        Matrix = matrix;
        RightHandSide = rightHandSide;
        Unknowns = unknowns;
    }

    public DenseMatrix Matrix { get; }

    public double[] RightHandSide { get; }

    public DcUnknowns Unknowns { get; }

    public static DcSystem Build(
        ImmutableArray<string> netNames,
        ImmutableArray<DcElement> elements,
        ImmutableArray<string> netsWithoutDcPath)
    {
        var unknowns = new DcUnknowns(netNames, elements);
        var system = new DcSystem(
            new DenseMatrix(unknowns.Size), new double[unknowns.Size], unknowns);

        for (var index = 0; index < elements.Length; index++)
        {
            system.Stamp(elements[index], unknowns.BranchOf(index));
        }

        foreach (var name in netsWithoutDcPath)
        {
            system.StampGmin(unknowns.NodeOf(name));
        }

        return system;
    }

    private void Stamp(DcElement element, int branch)
    {
        var plus = Unknowns.NodeOf(element.FirstNet);
        var minus = Unknowns.NodeOf(element.SecondNet);

        if (element.Conductance is { } conductance)
        {
            StampConductance(plus, minus, conductance);
        }

        if (element.Branch is { } definition)
        {
            StampBranch(branch, plus, minus, definition);
        }

        if (element.SourceCurrent != 0)
        {
            StampCurrent(plus, minus, element.SourceCurrent);
        }
    }

    private void StampConductance(int plus, int minus, double conductance)
    {
        if (plus >= 0)
        {
            Matrix.Add(plus, plus, conductance);
        }

        if (minus >= 0)
        {
            Matrix.Add(minus, minus, conductance);
        }

        if (plus >= 0 && minus >= 0)
        {
            Matrix.Add(plus, minus, -conductance);
            Matrix.Add(minus, plus, -conductance);
        }
    }

    private void StampBranch(int branch, int plus, int minus, DcBranch definition)
    {
        if (plus >= 0)
        {
            Matrix.Add(plus, branch, 1);
            Matrix.Add(branch, plus, 1);
        }

        if (minus >= 0)
        {
            Matrix.Add(minus, branch, -1);
            Matrix.Add(branch, minus, -1);
        }

        Matrix.Add(branch, branch, -definition.Resistance);
        RightHandSide[branch] += definition.Voltage;
    }

    private void StampCurrent(int plus, int minus, double current)
    {
        if (plus >= 0)
        {
            RightHandSide[plus] += current;
        }

        if (minus >= 0)
        {
            RightHandSide[minus] -= current;
        }
    }

    private void StampGmin(int node)
    {
        if (node >= 0)
        {
            Matrix.Add(node, node, DcAnalysis.Gmin);
        }
    }
}
