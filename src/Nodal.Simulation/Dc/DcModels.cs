using System.Collections.Immutable;

namespace Nodal.Simulation.Dc;

internal static class DcModels
{
    public const string FirstPassivePin = "1";
    public const string SecondPassivePin = "2";
    public const string PlusPin = "plus";
    public const string MinusPin = "minus";

    private static readonly Dictionary<string, DcModelKind> KindByDefinitionId =
        new(StringComparer.Ordinal)
        {
            ["resistor"] = DcModelKind.Resistor,
            ["capacitor"] = DcModelKind.Capacitor,
            ["inductor"] = DcModelKind.Inductor,
            ["voltage-source"] = DcModelKind.VoltageSource,
            ["current-source"] = DcModelKind.CurrentSource,
            ["ground"] = DcModelKind.Ground,
        };

    public static bool TryGetKind(string definitionId, out DcModelKind kind) =>
        KindByDefinitionId.TryGetValue(definitionId, out kind);

    public static ImmutableArray<string> PinsOf(DcModelKind kind) => kind switch
    {
        DcModelKind.VoltageSource or DcModelKind.CurrentSource => [PlusPin, MinusPin],
        DcModelKind.Ground => [],
        _ => [FirstPassivePin, SecondPassivePin],
    };
}
