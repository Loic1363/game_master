using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class DiodeDevice : NonlinearDevice
{
    private readonly int _anode;
    private readonly int _cathode;

    public DiodeDevice(int anode, int cathode)
    {
        _anode = anode;
        _cathode = cathode;
    }

    public override int StateCount => 1;

    public override int CurrentCount => 1;

    public override int PinCount => 2;

    public override void Measure(double[] solution, Span<double> state) =>
        state[0] = VoltageAt(_anode, solution) - VoltageAt(_cathode, solution);

    public override bool Limit(
        ReadOnlySpan<double> raw, ReadOnlySpan<double> previous, Span<double> limited)
    {
        limited[0] = DiodeModel.Limit(raw[0], previous[0], out var wasLimited);

        return wasLimited;
    }

    public override void Stamp(MnaStamps stamps, ReadOnlySpan<double> state) => stamps.Junction(
        _anode,
        _cathode,
        DiodeModel.Conductance(state[0]),
        DiodeModel.EquivalentCurrent(state[0]));

    public override void Currents(ReadOnlySpan<double> state, Span<double> currents) =>
        currents[0] = DiodeModel.Current(state[0]);

    public override void PinCurrents(ReadOnlySpan<double> state, Span<double> currents)
    {
        currents[0] = DiodeModel.Current(state[0]);
        currents[1] = -currents[0];
    }

    public override double PrincipalCurrent(ReadOnlySpan<double> state) =>
        DiodeModel.Current(state[0]);

    public override DeviceOperatingPoint? Describe(ReadOnlySpan<double> state) => null;
}
