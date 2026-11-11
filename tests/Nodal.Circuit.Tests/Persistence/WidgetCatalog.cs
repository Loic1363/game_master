using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Schedules;

namespace Nodal.Circuit.Tests.Persistence;

internal static class WidgetCatalog
{
    public const string DefinitionId = "widget";
    public const string BoundedRequired = "level";
    public const string BoundedOptional = "trim";
    public const string SeriesOptional = "tuned";

    public const string ButtonId = "button";
    public const string ButtonSchedule = "presses";
    public const string ButtonResistance = "on-resistance";

    public static Quantity RequiredDefault => new(5, Unit.Volt);

    public static ComponentCatalog Catalog { get; } = new([Widget(), Button()]);

    private static ComponentDefinition Button() => new(
        ButtonId,
        "Button",
        "A button used by the persistence tests.",
        ComponentCategory.Source,
        "SW",
        [new PinDefinition("a", -2, 0), new PinDefinition("b", 2, 0)],
        [
            new ParameterDefinition(
                ButtonSchedule,
                "Presses",
                ParameterTier.Basic,
                Unit.Second,
                ParameterValue.Of(PressSchedule.Released, Unit.Second),
                null,
                null,
                null,
                ParameterKind.Schedule),
            new ParameterDefinition(
                ButtonResistance,
                "On resistance",
                ParameterTier.Advanced,
                Unit.Ohm,
                new Quantity(0.01, Unit.Ohm),
                new Quantity(1e-3, Unit.Ohm),
                null,
                null),
        ]);

    private static ComponentDefinition Widget() => new(
        DefinitionId,
        "Widget",
        "A widget used by the persistence tests.",
        ComponentCategory.Passive,
        "W",
        [new PinDefinition("1", -2, 0), new PinDefinition("2", 2, 0)],
        [
            new ParameterDefinition(
                BoundedRequired,
                "Level",
                ParameterTier.Basic,
                Unit.Volt,
                RequiredDefault,
                new Quantity(0, Unit.Volt),
                new Quantity(10, Unit.Volt),
                null),
            new ParameterDefinition(
                BoundedOptional,
                "Trim",
                ParameterTier.Advanced,
                Unit.Volt,
                null,
                new Quantity(0, Unit.Volt),
                new Quantity(10, Unit.Volt),
                null),
            new ParameterDefinition(
                SeriesOptional,
                "Tuned",
                ParameterTier.Advanced,
                Unit.Ohm,
                null,
                null,
                null,
                StandardSeries.E12),
        ]);
}
