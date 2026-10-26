using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

public static class TransientAnalysis
{
    public static TransientResult Run(
        Netlist netlist,
        ComponentCatalog catalog,
        TransientOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(options);

        var refusals = DcAnalysis.Refusals(netlist, catalog);

        if (refusals.Length > 0)
        {
            return TransientResult.Refused(refusals);
        }

        return TransientStepper.Run(
            Elaborator.Elaborate(netlist, catalog), options, progress, cancellation);
    }
}
