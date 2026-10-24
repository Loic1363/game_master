using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Dc;

internal static class RatingChecks
{
    public static SimulationDiagnostic? Of(DcElement element, ComponentOperatingPoint result) =>
        element.Kind switch
        {
            DcModelKind.Resistor => Exceeded(
                element,
                result.Power.Value,
                element.Ratings.Power,
                Unit.Watt,
                "dissipates",
                "power"),

            DcModelKind.Capacitor => Exceeded(
                element,
                result.Voltage.Value,
                element.Ratings.Voltage,
                Unit.Volt,
                "holds",
                "voltage"),

            DcModelKind.Inductor => Exceeded(
                element,
                result.Current.Value,
                element.Ratings.Current,
                Unit.Ampere,
                "carries",
                "current"),

            _ => null,
        };

    private static SimulationDiagnostic? Exceeded(
        DcElement element,
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
            element.Id,
            $"{element.Designator} {verb} {new Quantity(Math.Abs(value), unit)}, above its "
            + $"{new Quantity(limit, unit)} {quantityName} rating.");
    }
}
