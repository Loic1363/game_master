using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class BipolarDevice : NonlinearDevice
{
    private const int BaseEmitter = 0;
    private const int BaseCollector = 1;

    private readonly bool _pType;
    private readonly int _collector;
    private readonly int _emitter;
    private readonly int _base;

    public BipolarDevice(bool pType, int collector, int emitter, int baseNode)
    {
        _pType = pType;
        _collector = collector;
        _emitter = emitter;
        _base = baseNode;
    }

    public override int StateCount => 2;

    public override int CurrentCount => 2;

    public override int PinCount => 3;

    public override void Measure(double[] solution, Span<double> state)
    {
        var baseVoltage = VoltageAt(_base, solution);

        state[BaseEmitter] = baseVoltage - VoltageAt(_emitter, solution);
        state[BaseCollector] = baseVoltage - VoltageAt(_collector, solution);
    }

    public override bool Limit(
        ReadOnlySpan<double> raw, ReadOnlySpan<double> previous, Span<double> limited)
    {
        limited[BaseEmitter] = BipolarModel.Limit(
            _pType, raw[BaseEmitter], previous[BaseEmitter]);

        limited[BaseCollector] = BipolarModel.Limit(
            _pType, raw[BaseCollector], previous[BaseCollector]);

        return limited[BaseEmitter] != raw[BaseEmitter]
            || limited[BaseCollector] != raw[BaseCollector];
    }

    public override void Stamp(MnaStamps stamps, ReadOnlySpan<double> state)
    {
        var baseEmitter = state[BaseEmitter];
        var baseCollector = state[BaseCollector];
        var bias = BipolarModel.Evaluate(_pType, baseEmitter, baseCollector);

        stamps.Junction(
            _base,
            _emitter,
            bias.EmitterJunctionConductance,
            bias.EmitterBranchCurrent - (bias.EmitterJunctionConductance * baseEmitter));

        stamps.Junction(
            _base,
            _collector,
            bias.CollectorJunctionConductance,
            bias.CollectorBranchCurrent
            - (bias.CollectorJunctionConductance * baseCollector));

        var equivalent = bias.TransportCurrent
            - (bias.ForwardTransconductance * baseEmitter)
            + (bias.ReverseTransconductance * baseCollector);

        stamps.Transconductance(
            _collector, _emitter, _base, _emitter, bias.ForwardTransconductance);

        stamps.Transconductance(
            _collector, _emitter, _base, _collector, -bias.ReverseTransconductance);

        stamps.Current(_collector, _emitter, -equivalent);
    }

    public override void Currents(ReadOnlySpan<double> state, Span<double> currents)
    {
        var bias = Bias(state);

        currents[0] = bias.CollectorCurrent;
        currents[1] = bias.BaseCurrent;
    }

    public override void PinCurrents(ReadOnlySpan<double> state, Span<double> currents)
    {
        var bias = Bias(state);

        currents[0] = bias.CollectorCurrent;
        currents[1] = bias.EmitterCurrent;
        currents[2] = bias.BaseCurrent;
    }

    public override double PrincipalCurrent(ReadOnlySpan<double> state) =>
        Bias(state).CollectorCurrent;

    public override DeviceOperatingPoint? Describe(ReadOnlySpan<double> state)
    {
        var bias = Bias(state);

        return DeviceDescriptions.Bipolar(
            _pType,
            state[BaseEmitter],
            state[BaseCollector],
            bias.CollectorCurrent,
            bias.BaseCurrent);
    }

    private BipolarBias Bias(ReadOnlySpan<double> state) =>
        BipolarModel.Evaluate(_pType, state[BaseEmitter], state[BaseCollector]);
}
