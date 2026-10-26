using Nodal.Simulation.LinearAlgebra;

namespace Nodal.Simulation.Mna;

internal sealed class MnaStamps
{
    public MnaStamps(int size)
    {
        Matrix = new DenseMatrix(size);
        RightHandSide = new double[size];
    }

    public DenseMatrix Matrix { get; }

    public double[] RightHandSide { get; }

    public void Conductance(int plus, int minus, double conductance)
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

    public void Current(int plus, int minus, double current)
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

    public void Branch(int branch, int plus, int minus, double resistance, double voltage)
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

        Matrix.Add(branch, branch, -resistance);
        RightHandSide[branch] += voltage;
    }

    public void Shunt(int node, double conductance)
    {
        if (node >= 0 && conductance > 0)
        {
            Matrix.Add(node, node, conductance);
        }
    }

    public void Junction(int plus, int minus, double conductance, double current)
    {
        Conductance(plus, minus, conductance);
        Current(plus, minus, -current);
    }
}
