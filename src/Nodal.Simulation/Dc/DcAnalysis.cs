using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

public static class DcAnalysis
{
    public static OperatingPoint Solve(
        Netlist netlist, ComponentCatalog catalog, DcOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(catalog);

        var refusals = Refusals(netlist, catalog);

        if (refusals.Length > 0)
        {
            return OperatingPoint.Refused(refusals);
        }

        var circuit = Elaborator.Elaborate(netlist, catalog);

        return Solve(circuit, options ?? DcOptions.Default);
    }

    internal static OperatingPoint Solve(ElaboratedCircuit circuit, DcOptions options) =>
        DcSolver.TrySolve(circuit, options, out var solution, out var failure)
            ? Solved(solution)
            : OperatingPoint.Refused([failure]);

    internal static ImmutableArray<SimulationDiagnostic> Refusals(
        Netlist netlist, ComponentCatalog catalog)
    {
        var diagnostics = ImmutableArray.CreateBuilder<SimulationDiagnostic>();

        foreach (var diagnostic in netlist.Diagnostics)
        {
            if (diagnostic.Severity == ConnectivitySeverity.Error)
            {
                diagnostics.Add(SimulationDiagnostic.Connectivity(diagnostic));
            }
        }

        if (!netlist.NetNames.Contains(SimulationDiagnostic.GroundNetName))
        {
            diagnostics.Add(SimulationDiagnostic.NoGround());
        }

        diagnostics.AddRange(UnsupportedComponents.In(netlist, catalog));

        return diagnostics.ToImmutable();
    }

    private static OperatingPoint Solved(DcSolution solution)
    {
        var circuit = solution.Circuit;
        var undefined = UndefinedNets(solution);
        var voltages = ImmutableSortedDictionary.CreateBuilder<string, Quantity>(
            StringComparer.Ordinal);

        foreach (var net in circuit.ExternalNets)
        {
            if (!undefined.Contains(net, StringComparer.Ordinal))
            {
                voltages[net] = new Quantity(solution.NodeVoltage(net), Unit.Volt);
            }
        }

        var components =
            ImmutableSortedDictionary.CreateBuilder<ComponentId, ComponentOperatingPoint>();
        var ratings = new List<SimulationDiagnostic>();

        foreach (var component in circuit.Components)
        {
            var result = ResultOf(solution, component);

            components[component.Id] = result;

            if (RatingChecks.Of(component, result) is { } warning)
            {
                ratings.Add(warning);
            }
        }

        return OperatingPoint.Solution(
            voltages.ToImmutable(),
            components.ToImmutable(),
            [.. undefined.Select(SimulationDiagnostic.NoDcPath), .. ratings],
            solution.Convergence);
    }

    private static ImmutableArray<string> UndefinedNets(DcSolution solution) =>
    [
        .. solution.UndefinedNodes.Where(
            node => !solution.Circuit.IsInternal(node)),
    ];

    private static ComponentOperatingPoint ResultOf(
        DcSolution solution, ElaboratedComponent component)
    {
        var across = solution.NodeVoltage(component.PlusNet)
            - solution.NodeVoltage(component.MinusNet);

        var current = component.TerminalPrimitives.Sum(solution.CurrentOf);

        return new ComponentOperatingPoint(
            component.Id,
            component.Designator,
            new Quantity(across, Unit.Volt),
            new Quantity(current, Unit.Ampere),
            new Quantity(across * current, Unit.Watt));
    }
}
