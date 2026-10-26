namespace Nodal.Simulation.Transient;

internal readonly record struct TransientIntegration(int Order, double Step)
{
    public const int BackwardEuler = 1;

    public const int Trapezoidal = 2;

    public double LeadingCoefficient => Order / Step;

    public double PreviousFlowWeight => Order - 1;
}
