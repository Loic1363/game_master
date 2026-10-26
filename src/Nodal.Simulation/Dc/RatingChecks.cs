using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal static class RatingChecks
{
    public static SimulationDiagnostic? Of(
        ElaboratedComponent component, ComponentOperatingPoint result) =>
        component.Kind switch
        {
            ComponentKind.Resistor => Exceeded(
                component,
                result.Power.Value,
                component.Ratings.Power,
                Unit.Watt,
                "dissipates",
                "power"),

            ComponentKind.Capacitor => Exceeded(
                component,
                result.Voltage.Value,
                component.Ratings.Voltage,
                Unit.Volt,
                "holds",
                "voltage"),

            ComponentKind.Inductor => Exceeded(
                component,
                result.Current.Value,
                component.Ratings.Current,
                Unit.Ampere,
                "carries",
                "current"),

            _ => null,
        };

    private static SimulationDiagnostic? Exceeded(
        ElaboratedComponent component,
        double value,
        double? rating,
        Unit unit,
        string verb,
        string quantityName)
    {
        if (rating is not { } limit || Math.Abs(value) <= limit)
        {
            return null;
        }

        return SimulationDiagnostic.RatingExceeded(
            component.Id,
            $"{component.Designator} {verb} {new Quantity(Math.Abs(value), unit)}, above its "
            + $"{new Quantity(limit, unit)} {quantityName} rating.");
    }
}
