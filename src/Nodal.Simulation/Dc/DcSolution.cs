using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Dc;

internal sealed class DcSolution
{
    private readonly double[] _values;
    private readonly double[] _deviceState;

    public DcSolution(
        ElaboratedCircuit circuit,
        ImmutableArray<DcElement> elements,
        MnaUnknowns unknowns,
        NonlinearDevices devices,
        ImmutableArray<string> undefinedNodes,
        double[] values,
        double[] deviceState,
        DcConvergence convergence)
    {
        Circuit = circuit;
        Elements = elements;
        Unknowns = unknowns;
        Devices = devices;
        UndefinedNodes = undefinedNodes;
        Convergence = convergence;
        _values = values;
        _deviceState = deviceState;
    }

    public ElaboratedCircuit Circuit { get; }

    public ImmutableArray<DcElement> Elements { get; }

    public MnaUnknowns Unknowns { get; }

    public NonlinearDevices Devices { get; }

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

        if (element.IsNonlinear)
        {
            return Devices.PrincipalCurrentOf(primitive, _deviceState);
        }

        if (element.IsControlledCurrent)
        {
            return -DeliveredCurrent(element);
        }

        var across = VoltageAcross(primitive);
        var branch = Unknowns.BranchOf(primitive);

        return (branch >= 0 ? _values[branch] : across * (element.Conductance ?? 0))
            - element.SourceCurrent;
    }

    public double[] PinCurrentsOf(int primitive) =>
        Devices.PinCurrentsOf(primitive, _deviceState);

    public double[] ControlledPinCurrents(int primitive)
    {
        var output = CurrentOf(primitive);
        var control = ControlCurrentOf(primitive);

        return [output, -output, control, -control];
    }

    public double ControlCurrentOf(int primitive) =>
        Elements[primitive].Control is { HasSense: true } control
            ? BranchCurrentOf(control.SenseElement)
            : 0;

    public double ControlVoltageOf(int primitive) =>
        Elements[primitive].Control is { } control
            ? NodeVoltage(control.PlusNode) - NodeVoltage(control.MinusNode)
            : 0;

    public bool IsClosedAt(int primitive, double time)
    {
        var element = Elements[primitive];

        return element.Kind == PrimitiveKind.Switch
            ? Devices.IsSwitchClosed(primitive, _deviceState)
            : element.Switch?.IsClosedAt(time) ?? false;
    }

    public double[] PinCurrentsAt(int primitive)
    {
        var element = Elements[primitive];

        if (element.IsNonlinear)
        {
            return PinCurrentsOf(primitive);
        }

        if (element.Control is not null)
        {
            return ControlledPinCurrents(primitive);
        }

        var current = CurrentOf(primitive);

        return [current, -current];
    }

    private double DeliveredCurrent(DcElement element) =>
        element.Gain * (element.Kind == PrimitiveKind.Vccs
            ? NodeVoltage(element.Control!.PlusNode) - NodeVoltage(element.Control.MinusNode)
            : BranchCurrentOf(element.Control!.SenseElement));

    private double BranchCurrentOf(int element)
    {
        var branch = Unknowns.BranchOf(element);

        return branch >= 0 ? _values[branch] : 0;
    }

    public DeviceOperatingPoint? DeviceOf(int primitive) =>
        Devices.DescribeOf(primitive, _deviceState);

    public double[] CopyDeviceState() => (double[])_deviceState.Clone();
}
