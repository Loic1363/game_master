using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;

namespace Nodal.Circuit.Connectivity;

public sealed record ConnectivityDiagnostic(
    ConnectivitySeverity Severity,
    ConnectivityDiagnosticCode Code,
    string Message,
    ImmutableArray<ComponentId> Components,
    ImmutableArray<WireId> Wires,
    ImmutableArray<PinRef> Pins,
    ImmutableArray<GridPoint> Positions)
{
    public const string GroundNetName = "0";

    public static ConnectivityDiagnostic FloatingPin(PinRef pin, string netName, GridPoint position) =>
        new(ConnectivitySeverity.Warning,
            ConnectivityDiagnosticCode.FloatingPin,
            $"The pin {pin} on net {netName} at {position} is not connected to any other pin.",
            [pin.ComponentId],
            [],
            [pin],
            [position]);

    public static ConnectivityDiagnostic DanglingWireEnd(WireId wire, GridPoint position) =>
        new(ConnectivitySeverity.Warning,
            ConnectivityDiagnosticCode.DanglingWireEnd,
            $"The wire {wire} ends at {position} without connecting to anything.",
            [],
            [wire],
            [],
            [position]);

    public static ConnectivityDiagnostic NoGround() =>
        new(ConnectivitySeverity.Warning,
            ConnectivityDiagnosticCode.NoGround,
            $"The schematic has no net named {GroundNetName}, so it has no ground reference.",
            [],
            [],
            [],
            []);

    public static ConnectivityDiagnostic GlobalNetShort(
        IEnumerable<string> netNames, ImmutableArray<PinRef> pins, ImmutableArray<WireId> wires)
    {
        var names = netNames.ToArray();

        return new ConnectivityDiagnostic(
            ConnectivitySeverity.Error,
            ConnectivityDiagnosticCode.GlobalNetShort,
            $"One net joins the global nets {string.Join(", ", names)}. It takes the name {names[0]}.",
            [.. pins.Select(pin => pin.ComponentId).Distinct()],
            wires,
            pins,
            []);
    }

    public static ConnectivityDiagnostic UnknownDefinition(ComponentId id, string definitionId) =>
        new(ConnectivitySeverity.Error,
            ConnectivityDiagnosticCode.UnknownDefinition,
            $"The component {id} has the unknown definition '{definitionId}', so its pins are skipped.",
            [id],
            [],
            [],
            []);

    public bool Equals(ConnectivityDiagnostic? other) =>
        other is not null
        && Severity == other.Severity
        && Code == other.Code
        && string.Equals(Message, other.Message, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Components, other.Components)
        && StructuralEquality.ItemsEqual(Wires, other.Wires)
        && StructuralEquality.ItemsEqual(Pins, other.Pins)
        && StructuralEquality.ItemsEqual(Positions, other.Positions);

    public override int GetHashCode() => HashCode.Combine(
        Severity,
        Code,
        Message,
        StructuralEquality.ItemsHash(Components),
        StructuralEquality.ItemsHash(Wires),
        StructuralEquality.ItemsHash(Pins),
        StructuralEquality.ItemsHash(Positions));

    public override string ToString() => $"{Severity} {Code}: {Message}";
}
