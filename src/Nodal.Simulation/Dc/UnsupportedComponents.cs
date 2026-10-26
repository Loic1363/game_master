using System.Collections.Immutable;
using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal static class UnsupportedComponents
{
    public static ImmutableArray<SimulationDiagnostic> In(
        Netlist netlist, ComponentCatalog catalog)
    {
        var diagnostics = ImmutableArray.CreateBuilder<SimulationDiagnostic>();

        foreach (var element in netlist.Elements)
        {
            if (!ComponentModels.IsSupported(element, out _))
            {
                diagnostics.Add(SimulationDiagnostic.UnsupportedComponent(
                    element.Id, element.Designator, NameOf(catalog, element.DefinitionId)));
            }
        }

        return diagnostics.ToImmutable();
    }

    private static string NameOf(ComponentCatalog catalog, string definitionId) =>
        catalog.TryGetById(definitionId, out var definition) ? definition.Name : definitionId;
}
