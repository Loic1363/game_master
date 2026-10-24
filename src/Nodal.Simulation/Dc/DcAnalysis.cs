using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.LinearAlgebra;

namespace Nodal.Simulation.Dc;

public static class DcAnalysis
{
    public const double Gmin = 1e-12;

    public static OperatingPoint Solve(Netlist netlist, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(catalog);

        var refusals = Refusals(netlist, catalog);

        if (refusals.Length > 0)
        {
            return OperatingPoint.Refused(refusals);
        }

        var elements = DcElements.Resolve(netlist, catalog);
        var undefinedNets = DcPaths.NetsWithoutPathToGround(netlist.NetNames, elements);
        var system = DcSystem.Build(netlist.NetNames, elements, undefinedNets);

        if (!LuDecomposition.TryFactor(system.Matrix, out var decomposition))
        {
            return OperatingPoint.Refused([Singular(netlist, elements)]);
        }

        var solution = decomposition.Solve(system.RightHandSide);

        return solution.All(double.IsFinite)
            ? Solved(netlist, elements, system.Unknowns, solution, undefinedNets)
            : OperatingPoint.Refused([Singular(netlist, elements)]);
    }

    private static ImmutableArray<SimulationDiagnostic> Refusals(
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

        foreach (var element in netlist.Elements)
        {
            if (!DcElements.IsSupported(element, out _))
            {
                diagnostics.Add(SimulationDiagnostic.UnsupportedComponent(
                    element.Id, element.Designator, NameOf(catalog, element.DefinitionId)));
            }
        }

        return diagnostics.ToImmutable();
    }

    private static SimulationDiagnostic Singular(Netlist netlist, ImmutableArray<DcElement> elements)
    {
        var cause = VoltageBranchCycles.Find(netlist.NetNames, elements);

        return SimulationDiagnostic.Singular(
            cause?.Description, cause?.Components ?? [], cause?.Nets ?? []);
    }

    private static OperatingPoint Solved(
        Netlist netlist,
        ImmutableArray<DcElement> elements,
        DcUnknowns unknowns,
        double[] solution,
        ImmutableArray<string> undefinedNets)
    {
        var voltages = ImmutableSortedDictionary.CreateBuilder<string, Quantity>(
            StringComparer.Ordinal);

        foreach (var name in netlist.NetNames)
        {
            voltages[name] = new Quantity(VoltageOf(unknowns, solution, name), Unit.Volt);
        }

        var components =
            ImmutableSortedDictionary.CreateBuilder<ComponentId, ComponentOperatingPoint>();
        var ratings = new List<SimulationDiagnostic>();

        for (var index = 0; index < elements.Length; index++)
        {
            var element = elements[index];
            var result = ResultOf(element, unknowns, solution, unknowns.BranchOf(index));

            components[element.Id] = result;

            if (RatingChecks.Of(element, result) is { } warning)
            {
                ratings.Add(warning);
            }
        }

        return OperatingPoint.Solution(
            voltages.ToImmutable(),
            components.ToImmutable(),
            [.. undefinedNets.Select(SimulationDiagnostic.NoDcPath), .. ratings]);
    }

    private static ComponentOperatingPoint ResultOf(
        DcElement element, DcUnknowns unknowns, double[] solution, int branch)
    {
        var across = VoltageOf(unknowns, solution, element.FirstNet)
            - VoltageOf(unknowns, solution, element.SecondNet);

        var current = (branch >= 0 ? solution[branch] : across * (element.Conductance ?? 0))
            - element.SourceCurrent;

        return new ComponentOperatingPoint(
            element.Id,
            element.Designator,
            new Quantity(across, Unit.Volt),
            new Quantity(current, Unit.Ampere),
            new Quantity(across * current, Unit.Watt));
    }

    private static double VoltageOf(DcUnknowns unknowns, double[] solution, string netName)
    {
        var node = unknowns.NodeOf(netName);

        return node >= 0 ? solution[node] : 0;
    }

    private static string NameOf(ComponentCatalog catalog, string definitionId) =>
        catalog.TryGetById(definitionId, out var definition) ? definition.Name : definitionId;
}
