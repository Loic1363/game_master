using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Waveforms;

public static class TransientFailureText
{
    public const string StaleText = "Out of date — press F10 to run again";
    public const string RefusedReason = "the analysis was refused";

    private const string StoppedPrefix = "Stopped at ";

    public static string? Of(TransientResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsPartial)
        {
            return null;
        }

        var time = new Quantity(result.FailureTime ?? result.FinalTime, Unit.Second);

        return $"{StoppedPrefix}{time}: {ReasonOf(result)}";
    }

    public static string ReasonOf(TransientResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var errors = result.Errors;

        return errors.Length > 0 ? ReasonOf(errors[0].Code) : RefusedReason;
    }

    public static string ReasonOf(SimulationDiagnosticCode code) => code switch
    {
        SimulationDiagnosticCode.TimestepTooSmall => "the timestep became too small",
        SimulationDiagnosticCode.NonConvergence => "the analysis stopped converging",
        SimulationDiagnosticCode.Singular => "the circuit has no unique solution",
        SimulationDiagnosticCode.UnboundedCurrent => "a current would be unbounded",
        SimulationDiagnosticCode.NoGround => "the circuit has no ground",
        SimulationDiagnosticCode.Connectivity => "the schematic is not connected",
        SimulationDiagnosticCode.UnsupportedComponent => "a component is not supported yet",
        _ => RefusedReason,
    };
}
