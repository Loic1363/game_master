using System.Collections.Immutable;
using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Elaboration;

internal static class SenseBranches
{
    public const int None = -1;

    public static int[] Of(ImmutableArray<Primitive> primitives)
    {
        var senseByOwner = new Dictionary<ComponentId, int>();
        var senseByPrimitive = new int[primitives.Length];

        Array.Fill(senseByPrimitive, None);

        for (var index = 0; index < primitives.Length; index++)
        {
            if (primitives[index].IsCurrentSense)
            {
                senseByOwner[primitives[index].Owner] = index;
            }
        }

        for (var index = 0; index < primitives.Length; index++)
        {
            if (primitives[index].IsCurrentControlled
                && senseByOwner.TryGetValue(primitives[index].Owner, out var sense))
            {
                senseByPrimitive[index] = sense;
            }
        }

        return senseByPrimitive;
    }
}
