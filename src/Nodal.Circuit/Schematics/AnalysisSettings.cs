using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Circuit.Schematics;

public sealed record AnalysisSettings
{
    public const int MaxProbes = 8;

    private readonly ImmutableArray<Probe> _probes;

    public AnalysisSettings(
        TransientSettings? transient = null, ImmutableArray<Probe> probes = default)
    {
        Transient = transient;
        Probes = probes;
    }

    public static AnalysisSettings None { get; } = new();

    public TransientSettings? Transient { get; init; }

    public ImmutableArray<Probe> Probes
    {
        get => _probes;
        init => _probes = value.IsDefault ? [] : value;
    }

    public bool IsEmpty => Transient is null && Probes.IsEmpty;

    public bool IsValid => Transient is null or { IsValid: true };

    public bool Equals(AnalysisSettings? other) =>
        other is not null
        && Transient == other.Transient
        && StructuralEquality.ItemsEqual(Probes, other.Probes);

    public override int GetHashCode() =>
        HashCode.Combine(Transient, StructuralEquality.ItemsHash(Probes));
}
