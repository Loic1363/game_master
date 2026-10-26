using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal sealed class TransientStorage
{
    private readonly ImmutableArray<Primitive> _primitives;
    private readonly ImmutableArray<int> _indices;

    private TransientStorage(
        ImmutableArray<Primitive> primitives, ImmutableArray<int> indices)
    {
        _primitives = primitives;
        _indices = indices;
    }

    public int Count => _indices.Length;

    public static TransientStorage Of(ElaboratedCircuit circuit)
    {
        var indices = ImmutableArray.CreateBuilder<int>();

        for (var index = 0; index < circuit.Primitives.Length; index++)
        {
            if (circuit.Primitives[index].StoresEnergy)
            {
                indices.Add(index);
            }
        }

        return new TransientStorage(circuit.Primitives, indices.ToImmutable());
    }

    public int PrimitiveOf(int element) => _indices[element];

    public bool IsCapacitor(int element) =>
        _primitives[_indices[element]].Kind == PrimitiveKind.Capacitor;

    public double Charge(int element, double across, double through) =>
        _primitives[_indices[element]].Value * (IsCapacitor(element) ? across : through);

    public double Flow(int element, double across, double through) =>
        IsCapacitor(element) ? through : across;
}
