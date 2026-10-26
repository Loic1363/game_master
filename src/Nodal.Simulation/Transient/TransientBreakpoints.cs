using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal sealed class TransientBreakpoints
{
    private readonly ImmutableArray<double> _times;

    private TransientBreakpoints(ImmutableArray<double> times) => _times = times;

    public ImmutableArray<double> Times => _times;

    public static TransientBreakpoints Of(ElaboratedCircuit circuit, TransientPlan plan)
    {
        var candidates = new List<double>();

        foreach (var primitive in circuit.Primitives)
        {
            if (primitive.Function is { } function)
            {
                candidates.AddRange(function.BreakpointsUntil(plan.StopTime));
            }
        }

        candidates.Add(plan.StopTime);
        candidates.Sort();

        var times = ImmutableArray.CreateBuilder<double>(candidates.Count);

        foreach (var candidate in candidates)
        {
            if (times.Count == 0 || candidate - times[^1] > plan.MinBreak)
            {
                times.Add(candidate);
            }
        }

        return new TransientBreakpoints(times.ToImmutable());
    }

    public double Next(double time)
    {
        foreach (var candidate in _times)
        {
            if (candidate > time)
            {
                return candidate;
            }
        }

        return _times[^1];
    }
}
