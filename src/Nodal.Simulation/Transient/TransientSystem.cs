using System.Collections.Immutable;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Mna;

namespace Nodal.Simulation.Transient;

internal sealed class TransientSystem
{
    private readonly ElaboratedCircuit _circuit;
    private readonly TransientStorage _storage;
    private readonly ImmutableArray<string> _shuntedNodes;
    private readonly TransientOptions _options;
    private readonly int[] _storageByPrimitive;

    public TransientSystem(
        ElaboratedCircuit circuit, TransientStorage storage, TransientOptions options)
    {
        _circuit = circuit;
        _storage = storage;
        _options = options;
        Unknowns = new MnaUnknowns(circuit.Nodes, [.. circuit.Primitives.Select(NeedsBranch)]);
        Devices = NonlinearDevices.Resolve(circuit.Primitives, Unknowns);

        _shuntedNodes = MnaPaths.NetsWithoutPathToGround(
            circuit.Nodes,
            circuit.Primitives.Where(Conducts).SelectMany(
                primitive => primitive.ConductionPaths));

        _storageByPrimitive = new int[circuit.Primitives.Length];
        Array.Fill(_storageByPrimitive, -1);

        for (var element = 0; element < storage.Count; element++)
        {
            _storageByPrimitive[storage.PrimitiveOf(element)] = element;
        }
    }

    public MnaUnknowns Unknowns { get; }

    public NonlinearDevices Devices { get; }

    public ImmutableArray<string> ShuntedNodes => _shuntedNodes;

    public static bool NeedsBranch(Primitive primitive) => primitive.Kind switch
    {
        PrimitiveKind.VoltageSource => true,
        PrimitiveKind.Inductor => true,
        PrimitiveKind.Resistor => primitive.Value <= 0,
        _ => false,
    };

    public MnaStamps Build(
        double time,
        TransientIntegration integration,
        TransientState previous,
        double[] deviceState)
    {
        var stamps = new MnaStamps(Unknowns.Size);

        for (var index = 0; index < _circuit.Primitives.Length; index++)
        {
            Stamp(stamps, index, time, integration, previous);
        }

        Devices.Stamp(stamps, deviceState);

        foreach (var node in _shuntedNodes)
        {
            stamps.Shunt(Unknowns.NodeOf(node), _options.Gmin);
        }

        return stamps;
    }

    public double CurrentOf(
        int index,
        double[] solution,
        double[] deviceState,
        double time,
        TransientIntegration integration,
        TransientState previous)
    {
        var branch = Unknowns.BranchOf(index);

        if (branch >= 0)
        {
            return solution[branch];
        }

        var primitive = _circuit.Primitives[index];

        if (primitive.IsNonlinear)
        {
            return Devices.PrincipalCurrentOf(index, deviceState);
        }

        var across = VoltageAcross(index, solution);

        return primitive.Kind switch
        {
            PrimitiveKind.Resistor => across / primitive.Value,
            PrimitiveKind.Capacitor => CapacitorCurrent(index, across, integration, previous),
            PrimitiveKind.CurrentSource => -primitive.ValueAt(time),
            _ => 0,
        };
    }

    public double[] PinCurrentsOf(int index, double[] deviceState) =>
        Devices.PinCurrentsOf(index, deviceState);

    public double VoltageAcross(int index, double[] solution) =>
        Unknowns.NodeVoltage(_circuit.Primitives[index].PlusNode, solution)
        - Unknowns.NodeVoltage(_circuit.Primitives[index].MinusNode, solution);

    private static bool Conducts(Primitive primitive) => primitive.Kind switch
    {
        PrimitiveKind.CurrentSource => false,
        PrimitiveKind.Capacitor => primitive.Value > 0,
        _ => true,
    };

    private double CapacitorCurrent(
        int index, double across, TransientIntegration integration, TransientState previous)
    {
        var element = _storageByPrimitive[index];
        var conductance = integration.LeadingCoefficient * _circuit.Primitives[index].Value;

        return (conductance * (across - previous.Across[element]))
            - (integration.PreviousFlowWeight * previous.Through[element]);
    }

    private void Stamp(
        MnaStamps stamps,
        int index,
        double time,
        TransientIntegration integration,
        TransientState previous)
    {
        var primitive = _circuit.Primitives[index];
        var plus = Unknowns.NodeOf(primitive.PlusNode);
        var minus = Unknowns.NodeOf(primitive.MinusNode);
        var branch = Unknowns.BranchOf(index);

        switch (primitive.Kind)
        {
            case PrimitiveKind.Resistor when primitive.Value > 0:
                stamps.Conductance(plus, minus, 1 / primitive.Value);

                break;

            case PrimitiveKind.Resistor:
                stamps.Branch(branch, plus, minus, 0, 0);

                break;

            case PrimitiveKind.Capacitor:
                StampCapacitor(stamps, index, plus, minus, integration, previous);

                break;

            case PrimitiveKind.Inductor:
                StampInductor(stamps, index, branch, plus, minus, integration, previous);

                break;

            case PrimitiveKind.VoltageSource:
                stamps.Branch(branch, plus, minus, 0, primitive.ValueAt(time));

                break;

            case PrimitiveKind.CurrentSource:
                stamps.Current(plus, minus, primitive.ValueAt(time));

                break;
        }
    }

    private void StampCapacitor(
        MnaStamps stamps,
        int index,
        int plus,
        int minus,
        TransientIntegration integration,
        TransientState previous)
    {
        var capacitance = _circuit.Primitives[index].Value;

        if (capacitance <= 0)
        {
            return;
        }

        var element = _storageByPrimitive[index];
        var conductance = integration.LeadingCoefficient * capacitance;
        var equivalent = (conductance * previous.Across[element])
            + (integration.PreviousFlowWeight * previous.Through[element]);

        stamps.Conductance(plus, minus, conductance);
        stamps.Current(plus, minus, equivalent);
    }

    private void StampInductor(
        MnaStamps stamps,
        int index,
        int branch,
        int plus,
        int minus,
        TransientIntegration integration,
        TransientState previous)
    {
        var element = _storageByPrimitive[index];
        var resistance = integration.LeadingCoefficient * _circuit.Primitives[index].Value;
        var voltage = -(resistance * previous.Through[element])
            - (integration.PreviousFlowWeight * previous.Across[element]);

        stamps.Branch(branch, plus, minus, resistance, voltage);
    }
}
