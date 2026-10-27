using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class MosfetDevice : NonlinearDevice
{
    private const int GateSource = 0;
    private const int DrainSource = 1;
    private const int BodyDiode = 2;

    private readonly bool _pChannel;
    private readonly int _drain;
    private readonly int _source;
    private readonly int _gate;

    public MosfetDevice(bool pChannel, int drain, int source, int gate)
    {
        _pChannel = pChannel;
        _drain = drain;
        _source = source;
        _gate = gate;
    }

    public override int StateCount => 3;

    public override int CurrentCount => 2;

    public override int PinCount => 3;

    public override void Measure(double[] solution, Span<double> state)
    {
        var source = VoltageAt(_source, solution);
        var drainSource = VoltageAt(_drain, solution) - source;

        state[GateSource] = VoltageAt(_gate, solution) - source;
        state[DrainSource] = drainSource;
        state[BodyDiode] = MosfetModel.BodyDiodeVoltage(_pChannel, drainSource);
    }

    public override bool Limit(
        ReadOnlySpan<double> raw, ReadOnlySpan<double> previous, Span<double> limited)
    {
        limited[GateSource] = MosfetModel.LimitGate(
            _pChannel, raw[GateSource], previous[GateSource]);

        limited[DrainSource] = MosfetModel.LimitDrainSource(
            _pChannel, raw[DrainSource], previous[DrainSource]);

        limited[BodyDiode] = DiodeModel.Limit(
            raw[BodyDiode], previous[BodyDiode], out var junctionLimited);

        return junctionLimited
            || limited[GateSource] != raw[GateSource]
            || limited[DrainSource] != raw[DrainSource];
    }

    public override void Stamp(MnaStamps stamps, ReadOnlySpan<double> state)
    {
        var gateSource = state[GateSource];
        var drainSource = state[DrainSource];
        var bias = MosfetModel.Evaluate(_pChannel, gateSource, drainSource);

        var equivalent = bias.ChannelCurrent
            - (bias.OutputConductance * drainSource)
            - (bias.Transconductance * gateSource);

        stamps.Conductance(_drain, _source, bias.OutputConductance);
        stamps.Transconductance(_drain, _source, _gate, _source, bias.Transconductance);
        stamps.Current(_drain, _source, -equivalent);

        stamps.Junction(
            Anode,
            Cathode,
            DiodeModel.Conductance(state[BodyDiode]),
            DiodeModel.EquivalentCurrent(state[BodyDiode]));
    }

    public override void Currents(ReadOnlySpan<double> state, Span<double> currents)
    {
        currents[0] = MosfetModel.Evaluate(
            _pChannel, state[GateSource], state[DrainSource]).ChannelCurrent;

        currents[1] = DiodeModel.Current(state[BodyDiode]);
    }

    public override void PinCurrents(ReadOnlySpan<double> state, Span<double> currents)
    {
        currents[0] = PrincipalCurrent(state);
        currents[1] = -currents[0];
        currents[2] = 0;
    }

    public override double PrincipalCurrent(ReadOnlySpan<double> state) =>
        MosfetModel.DrainCurrent(
            _pChannel, state[GateSource], state[DrainSource], state[BodyDiode]);

    public override DeviceOperatingPoint? Describe(ReadOnlySpan<double> state) =>
        DeviceDescriptions.Mosfet(
            _pChannel, state[GateSource], state[DrainSource], PrincipalCurrent(state));

    private int Anode => _pChannel ? _drain : _source;

    private int Cathode => _pChannel ? _source : _drain;
}
