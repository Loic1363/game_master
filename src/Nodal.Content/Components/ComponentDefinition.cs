namespace Nodal.Content.Components;

public sealed record ComponentDefinition(
    string Id,
    string Name,
    string Description,
    ComponentCategory Category,
    string DesignatorPrefix,
    IReadOnlyList<PinDefinition> Pins,
    IReadOnlyList<ParameterDefinition> Parameters);
