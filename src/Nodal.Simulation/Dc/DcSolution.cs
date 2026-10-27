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

        var across = VoltageAcross(primitive);
        var branch = Unknowns.BranchOf(primitive);

        return (branch >= 0 ? _values[branch] : across * (element.Conductance ?? 0))
            - element.SourceCurrent;
    }

    public double[] PinCurrentsOf(int primitive) =>
        Devices.PinCurrentsOf(primitive, _deviceState);

    public DeviceOperatingPoint? DeviceOf(int primitive) =>
        Devices.DescribeOf(primitive, _deviceState);

    public double[] CopyDeviceState() => (double[])_deviceState.Clone();
}
