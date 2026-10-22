using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;

namespace Nodal.Circuit.Persistence;

public sealed record SchematicReadResult(
    Schematic? Schematic, ImmutableArray<SchematicDiagnostic> Diagnostics)
{
    [MemberNotNullWhen(true, nameof(Schematic))]
    public bool Loaded => Schematic is not null;

    public ImmutableArray<SchematicDiagnostic> Errors =>
        [.. Diagnostics.Where(diagnostic => diagnostic.Severity == SchematicSeverity.Error)];

    public ImmutableArray<SchematicDiagnostic> Warnings =>
        [.. Diagnostics.Where(diagnostic => diagnostic.Severity == SchematicSeverity.Warning)];

    public bool Equals(SchematicReadResult? other) =>
        other is not null
        && Schematic == other.Schematic
        && StructuralEquality.ItemsEqual(Diagnostics, other.Diagnostics);

    public override int GetHashCode() =>
        HashCode.Combine(Schematic, StructuralEquality.ItemsHash(Diagnostics));
}
