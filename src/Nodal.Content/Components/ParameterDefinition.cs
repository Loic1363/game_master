using Nodal.Foundation.Quantities;

namespace Nodal.Content.Components;

public sealed record ParameterDefinition(
    string Name,
    string Label,
    ParameterTier Tier,
    Unit Unit,
    ParameterValue? Default,
    Quantity? Minimum,
    Quantity? Maximum,
    StandardSeries? Series,
    ParameterKind Kind = ParameterKind.Quantity)
{
    public bool IsRequired => Default is not null;

    public bool IsWaveform => Kind == ParameterKind.Waveform;

    public bool IsSchedule => Kind == ParameterKind.Schedule;

    public Quantity? DefaultQuantity =>
        Default is { IsQuantity: true } value ? value.Quantity : null;
}
