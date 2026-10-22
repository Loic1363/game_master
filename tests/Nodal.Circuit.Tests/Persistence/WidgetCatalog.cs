using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Tests.Persistence;

internal static class WidgetCatalog
{
    public const string DefinitionId = "widget";
    public const string BoundedRequired = "level";
    public const string BoundedOptional = "trim";
    public const string SeriesOptional = "tuned";

    public static Quantity RequiredDefault => new(5, Unit.Volt);

    public static ComponentCatalog Catalog { get; } = new([Widget()]);

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
