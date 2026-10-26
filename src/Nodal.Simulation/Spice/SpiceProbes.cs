using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Spice;

public static class SpiceProbes
{
    public static bool TryPlan(
        Netlist netlist,
        ComponentCatalog catalog,
        [NotNullWhen(true)] out SpiceProbePlan? plan,
        out ImmutableArray<SimulationDiagnostic> refusals)
    {
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!SpiceExporter.TryExport(netlist, catalog, out _, out refusals))
        {
            plan = null;

            return false;
        }

        var circuit = Elaborator.Elaborate(netlist, catalog);
        var components = ImmutableArray.CreateBuilder<SpiceProbe>(circuit.Components.Length);

        foreach (var component in circuit.Components)
        {
            components.Add(new SpiceProbe(component.Designator, ElementsOf(circuit, component)));
        }

        plan = new SpiceProbePlan(circuit.ExternalNets, components.ToImmutable());

        return true;
    }

    private static ImmutableArray<string> ElementsOf(
        ElaboratedCircuit circuit, ElaboratedComponent component) =>
    [
        .. component.TerminalPrimitives.Select(
            index => SpiceElements.NameOf(circuit.Primitives[index])),
    ];
}
