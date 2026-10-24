using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;

namespace Nodal.Simulation.Diagnostics;

public sealed record SimulationDiagnostic(
    SimulationSeverity Severity,
    SimulationDiagnosticCode Code,
    string Message,
    ImmutableArray<ComponentId> Components,
    ImmutableArray<string> Nets)
{
    public const string GroundNetName = "0";

    private const string UnidentifiedSingularCause =
        "The circuit has no unique DC solution; the matrix is singular and no structural cause "
        + "was found.";

    public static SimulationDiagnostic Connectivity(ConnectivityDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new SimulationDiagnostic(
            SimulationSeverity.Error,
            SimulationDiagnosticCode.Connectivity,
            diagnostic.Message,
            diagnostic.Components,
            []);
    }

    public static SimulationDiagnostic NoGround() =>
        new(SimulationSeverity.Error,
            SimulationDiagnosticCode.NoGround,
            $"The circuit has no net named {GroundNetName}, so the DC analysis has no voltage "
            + "reference.",
            [],
            []);

    public static SimulationDiagnostic UnsupportedComponent(
        ComponentId id, string designator, string definitionName) =>
        new(SimulationSeverity.Error,
            SimulationDiagnosticCode.UnsupportedComponent,
            $"{designator} ({definitionName}) is not supported by the simulator yet.",
            [id],
            []);

    public static SimulationDiagnostic NoDcPath(string netName) =>
        new(SimulationSeverity.Warning,
            SimulationDiagnosticCode.NoDcPath,
            $"{netName} has no DC path to ground; its voltage is undefined (shown as 0 V).",
            [],
            [netName]);

    public static SimulationDiagnostic Singular(
        string? cause, ImmutableArray<ComponentId> components, ImmutableArray<string> nets) =>
        new(SimulationSeverity.Error,
            SimulationDiagnosticCode.Singular,
            cause is null
                ? UnidentifiedSingularCause
                : $"The circuit has no unique DC solution: {cause}",
            components,
            nets);

    public static SimulationDiagnostic RatingExceeded(ComponentId id, string message) =>
        new(SimulationSeverity.Warning, SimulationDiagnosticCode.RatingExceeded, message, [id], []);

    public bool Equals(SimulationDiagnostic? other) =>
        other is not null
        && Severity == other.Severity
        && Code == other.Code
        && string.Equals(Message, other.Message, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Components, other.Components)
        && StructuralEquality.ItemsEqual(Nets, other.Nets);

    public override int GetHashCode() => HashCode.Combine(
        Severity,
        Code,
        Message,
        StructuralEquality.ItemsHash(Components),
        StructuralEquality.ItemsHash(Nets));

    public override string ToString() => $"{Severity} {Code}: {Message}";
}
