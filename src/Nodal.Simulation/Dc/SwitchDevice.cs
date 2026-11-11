using Nodal.Circuit.Schematics;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class SwitchDevice : NonlinearDevice
{
    private const int Across = 0;
    private const int Control = 1;
    private const int Closed = 2;
    private const double ClosedFlag = 1;
    private const double OpenFlag = 0;

    private readonly SwitchSettings _settings;
    private readonly int _plus;
    private readonly int _minus;
    private readonly int _controlPlus;
    private readonly int _controlMinus;

    public SwitchDevice(
        SwitchIdentity identity,
        SwitchSettings settings,
        int plus,
        int minus,
        int controlPlus,
        int controlMinus)
    {
        Identity = identity;
        _settings = settings;
        _plus = plus;
        _minus = minus;
        _controlPlus = controlPlus;
        _controlMinus = controlMinus;
    }

    public SwitchIdentity Identity { get; }

    public override int StateCount => 3;

    public override int CurrentCount => 1;

    public override int PinCount => 4;

    public static bool IsClosed(ReadOnlySpan<double> state) => state[Closed] != OpenFlag;

    public override void Measure(double[] solution, Span<double> state)
    {
        state[Across] = VoltageAt(_plus, solution) - VoltageAt(_minus, solution);
        state[Control] = VoltageAt(_controlPlus, solution) - VoltageAt(_controlMinus, solution);
        state[Closed] = OpenFlag;
    }

    public override bool Limit(
        ReadOnlySpan<double> raw, ReadOnlySpan<double> previous, Span<double> limited)
    {
        limited[Across] = raw[Across];
        limited[Control] = raw[Control];
        limited[Closed] = NextClosed(raw[Control], IsClosed(previous)) ? ClosedFlag : OpenFlag;

        return limited[Closed] != previous[Closed];
    }

    public override void Stamp(MnaStamps stamps, ReadOnlySpan<double> state) =>
        stamps.Conductance(_plus, _minus, 1 / ResistanceOf(state));

    public override void Currents(ReadOnlySpan<double> state, Span<double> currents) =>
        currents[0] = PrincipalCurrent(state);

    public override void PinCurrents(ReadOnlySpan<double> state, Span<double> currents)
    {
        currents[0] = PrincipalCurrent(state);
        currents[1] = -currents[0];
        currents[2] = 0;
        currents[3] = 0;
    }

    public override double PrincipalCurrent(ReadOnlySpan<double> state) =>
        state[Across] / ResistanceOf(state);

    public override DeviceOperatingPoint? Describe(ReadOnlySpan<double> state) =>
        DeviceDescriptions.Switch(
            IsClosed(state), state[Control], state[Across], PrincipalCurrent(state));

    private bool NextClosed(double control, bool closed)
    {
        if (control > _settings.Threshold + _settings.Hysteresis)
        {
            return true;
        }

        return control >= _settings.Threshold - _settings.Hysteresis && closed;
    }

    private double ResistanceOf(ReadOnlySpan<double> state) =>
        _settings.ResistanceOf(IsClosed(state));
}
