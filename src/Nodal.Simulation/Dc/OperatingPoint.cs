using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Dc;

public sealed class OperatingPoint
{
    private const string RefusedText = "refused";

    private OperatingPoint(
        bool isSolved,
        ImmutableSortedDictionary<string, Quantity> netVoltages,
        ImmutableSortedDictionary<ComponentId, ComponentOperatingPoint> components,
        ImmutableArray<SimulationDiagnostic> diagnostics)
    {
        IsSolved = isSolved;
        NetVoltages = netVoltages;
        Components = components;
        Diagnostics = diagnostics;
    }

    public bool IsSolved { get; }

    public ImmutableSortedDictionary<string, Quantity> NetVoltages { get; }

    public ImmutableSortedDictionary<ComponentId, ComponentOperatingPoint> Components { get; }

    public ImmutableArray<SimulationDiagnostic> Diagnostics { get; }

    public ImmutableArray<SimulationDiagnostic> Errors =>
        [.. Diagnostics.Where(diagnostic => diagnostic.Severity == SimulationSeverity.Error)];

    public ImmutableArray<SimulationDiagnostic> Warnings =>
        [.. Diagnostics.Where(diagnostic => diagnostic.Severity == SimulationSeverity.Warning)];

    public ImmutableArray<SimulationDiagnostic> RatingWarnings =>
    [
        .. Diagnostics.Where(
            diagnostic => diagnostic.Code == SimulationDiagnosticCode.RatingExceeded),
    ];

    public ImmutableArray<string> UndefinedNets =>
    [
        .. Diagnostics
            .Where(diagnostic => diagnostic.Code == SimulationDiagnosticCode.NoDcPath)
            .SelectMany(diagnostic => diagnostic.Nets),
    ];

    public bool TryGetNetVoltage(string netName, out Quantity voltage) =>
        NetVoltages.TryGetValue(netName, out voltage);

    public bool TryGetComponent(
        ComponentId id, [NotNullWhen(true)] out ComponentOperatingPoint? component) =>
        Components.TryGetValue(id, out component);

    public bool HasRatingWarning(ComponentId id) =>
        RatingWarnings.Any(diagnostic => diagnostic.Components.Contains(id));

    public bool IsUndefined(string netName) =>
        UndefinedNets.Contains(netName, StringComparer.Ordinal);

    public string ToDebugText()
    {
        var text = new StringBuilder();

        if (!IsSolved)
        {
            text.Append(RefusedText);
        }

        foreach (var entry in NetVoltages)
        {
            Line(text).Append("net ").Append(entry.Key).Append(" = ").Append(entry.Value);
        }

        foreach (var entry in Components)
        {
            Line(text).Append(entry.Value);
        }

        foreach (var diagnostic in Diagnostics)
        {
            Line(text).Append(diagnostic);
        }

        return text.ToString();
    }

    internal static OperatingPoint Refused(ImmutableArray<SimulationDiagnostic> diagnostics) =>
        new(false,
            ImmutableSortedDictionary.Create<string, Quantity>(StringComparer.Ordinal),
            ImmutableSortedDictionary<ComponentId, ComponentOperatingPoint>.Empty,
            diagnostics);

    internal static OperatingPoint Solution(
        ImmutableSortedDictionary<string, Quantity> netVoltages,
        ImmutableSortedDictionary<ComponentId, ComponentOperatingPoint> components,
        ImmutableArray<SimulationDiagnostic> diagnostics) =>
        new(true, netVoltages, components, diagnostics);

    private static StringBuilder Line(StringBuilder text)
    {
        if (text.Length > 0)
        {
            text.Append('\n');
        }

        return text;
    }
}
