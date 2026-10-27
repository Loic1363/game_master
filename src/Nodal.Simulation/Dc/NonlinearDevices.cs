using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class NonlinearDevices
{
    private readonly ImmutableArray<NonlinearDevice> _devices;
    private readonly int[] _deviceByPrimitive;
    private readonly int[] _stateOffsets;
    private readonly int[] _currentOffsets;

    private NonlinearDevices(
        ImmutableArray<NonlinearDevice> devices, int[] deviceByPrimitive)
    {
        _devices = devices;
        _deviceByPrimitive = deviceByPrimitive;
        _stateOffsets = new int[devices.Length];
        _currentOffsets = new int[devices.Length];

        for (var index = 0; index < devices.Length; index++)
        {
            _stateOffsets[index] = StateCount;
            _currentOffsets[index] = CurrentCount;
            StateCount += devices[index].StateCount;
            CurrentCount += devices[index].CurrentCount;
        }
    }

    public int StateCount { get; }

    public int CurrentCount { get; }

    public bool Any => _devices.Length > 0;

    public static NonlinearDevices Resolve(
        ImmutableArray<Primitive> primitives, MnaUnknowns unknowns)
    {
        var devices = ImmutableArray.CreateBuilder<NonlinearDevice>();
        var deviceByPrimitive = new int[primitives.Length];

        Array.Fill(deviceByPrimitive, -1);

        for (var index = 0; index < primitives.Length; index++)
        {
            if (Of(primitives[index], unknowns) is not { } device)
            {
                continue;
            }

            deviceByPrimitive[index] = devices.Count;
            devices.Add(device);
        }

        return new NonlinearDevices(devices.ToImmutable(), deviceByPrimitive);
    }

    public double[] Measure(double[] solution)
    {
        var state = new double[StateCount];

        for (var index = 0; index < _devices.Length; index++)
        {
            _devices[index].Measure(solution, StateOf(state, index));
        }

        return state;
    }

    public double[] Limited(double[] raw, double[] previous, out bool limitedAny)
    {
        var limited = new double[StateCount];

        limitedAny = false;

        for (var index = 0; index < _devices.Length; index++)
        {
            limitedAny |= _devices[index].Limit(
                StateOf(raw, index), StateOf(previous, index), StateOf(limited, index));
        }

        return limited;
    }

    public void Stamp(MnaStamps stamps, double[] state)
    {
        for (var index = 0; index < _devices.Length; index++)
        {
            _devices[index].Stamp(stamps, StateOf(state, index));
        }
    }

    public double[] Currents(double[] state)
    {
        var currents = new double[CurrentCount];

        for (var index = 0; index < _devices.Length; index++)
        {
            _devices[index].Currents(
                StateOf(state, index),
                currents.AsSpan(_currentOffsets[index], _devices[index].CurrentCount));
        }

        return currents;
    }

    public bool Has(int primitive) => _deviceByPrimitive[primitive] >= 0;

    public DeviceOperatingPoint? DescribeOf(int primitive, double[] state)
    {
        var index = _deviceByPrimitive[primitive];

        return _devices[index].Describe(StateOf(state, index));
    }

    public double PrincipalCurrentOf(int primitive, double[] state)
    {
        var device = _deviceByPrimitive[primitive];

        return _devices[device].PrincipalCurrent(StateOf(state, device));
    }

    public double[] PinCurrentsOf(int primitive, double[] state)
    {
        var index = _deviceByPrimitive[primitive];
        var device = _devices[index];
        var currents = new double[device.PinCount];

        device.PinCurrents(StateOf(state, index), currents);

        return currents;
    }

    private static NonlinearDevice? Of(Primitive primitive, MnaUnknowns unknowns)
    {
        var first = unknowns.NodeOf(primitive.PlusNode);
        var second = unknowns.NodeOf(primitive.MinusNode);

        return primitive.Kind switch
        {
            PrimitiveKind.Diode => new DiodeDevice(first, second),

            PrimitiveKind.Nmos or PrimitiveKind.Pmos => new MosfetDevice(
                primitive.Kind == PrimitiveKind.Pmos,
                first,
                second,
                unknowns.NodeOf(primitive.ControlNode)),

            PrimitiveKind.Npn or PrimitiveKind.Pnp => new BipolarDevice(
                primitive.Kind == PrimitiveKind.Pnp,
                first,
                second,
                unknowns.NodeOf(primitive.ControlNode)),

            _ => null,
        };
    }

    private Span<double> StateOf(double[] state, int device) =>
        state.AsSpan(_stateOffsets[device], _devices[device].StateCount);
}
