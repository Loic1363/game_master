using System.Collections.Immutable;
using System.Text;
using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Foundation;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Spice;

public static class SpiceExporter
{
    public const string Extension = ".cir";

    public static string Export(Netlist netlist, ComponentCatalog catalog)
    {
        if (TryExport(netlist, catalog, out var deck, out var refusals))
        {
            return deck;
        }

        throw new InvalidOperationException(
            "The netlist cannot be exported: "
            + string.Join(" ", refusals.Select(diagnostic => diagnostic.Message)));
    }

    public static bool TryExport(
        Netlist netlist,
        ComponentCatalog catalog,
        out string deck,
        out ImmutableArray<SimulationDiagnostic> refusals)
    {
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(catalog);

        deck = string.Empty;
        refusals = Refusals(netlist, catalog);

        if (refusals.Length > 0)
        {
            return false;
        }

        var circuit = Elaborator.Elaborate(netlist, catalog);

        refusals = UnsupportedWaveforms(circuit);

        if (refusals.Length > 0)
        {
            return false;
        }

        deck = Deck(circuit);

        return true;
    }

    private static ImmutableArray<SimulationDiagnostic> Refusals(
        Netlist netlist, ComponentCatalog catalog)
    {
        var diagnostics = ImmutableArray.CreateBuilder<SimulationDiagnostic>();

        if (!netlist.NetNames.Contains(SimulationDiagnostic.GroundNetName))
        {
            diagnostics.Add(SimulationDiagnostic.NoGround());
        }

        diagnostics.AddRange(UnsupportedComponents.In(netlist, catalog));

        return diagnostics.ToImmutable();
    }

    private static ImmutableArray<SimulationDiagnostic> UnsupportedWaveforms(
        ElaboratedCircuit circuit)
    {
        var diagnostics = ImmutableArray.CreateBuilder<SimulationDiagnostic>();

        foreach (var primitive in circuit.Primitives)
        {
            if (primitive.Function is { } function && !SpiceWaveforms.IsExportable(function))
            {
                diagnostics.Add(SimulationDiagnostic.UnsupportedWaveformFeature(
                    primitive.Owner,
                    primitive.Designator,
                    SpiceWaveforms.UnsupportedFeatureOf(function)));
            }
        }

        return diagnostics.ToImmutable();
    }

    private static string Deck(ElaboratedCircuit circuit)
    {
        var text = new StringBuilder();

        Append(text, Title());

        foreach (var primitive in circuit.Primitives)
        {
            Append(text, SpiceElements.Of(primitive));
        }

        Append(text, SpiceFormat.ModelLine);
        Append(text, SpiceFormat.OptionsLine);
        Append(text, SpiceFormat.EndLine);

        return text.ToString();
    }

    private static string Title() =>
        SpiceFormat.TitlePrefix + NodalInfo.Name + " " + NodalInfo.Version
        + SpiceFormat.TitleSuffix;

    private static void Append(StringBuilder text, string line) =>
        text.Append(line).Append(SpiceFormat.LineSeparator);
}
