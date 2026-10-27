using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal abstract class NonlinearDevice
{
    public abstract int StateCount { get; }

    public abstract int CurrentCount { get; }

    public abstract int PinCount { get; }

    public abstract void Measure(double[] solution, Span<double> state);

    public abstract bool Limit(
        ReadOnlySpan<double> raw, ReadOnlySpan<double> previous, Span<double> limited);

    public abstract void Stamp(MnaStamps stamps, ReadOnlySpan<double> state);

    public abstract void Currents(ReadOnlySpan<double> state, Span<double> currents);

    public abstract void PinCurrents(ReadOnlySpan<double> state, Span<double> currents);

    public abstract double PrincipalCurrent(ReadOnlySpan<double> state);

    public abstract DeviceOperatingPoint? Describe(ReadOnlySpan<double> state);

    protected static double VoltageAt(int node, double[] solution) =>
        node >= 0 ? solution[node] : 0;
}
