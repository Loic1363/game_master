using Nodal.Foundation.Quantities;

namespace Nodal.Content.Components;

public sealed record ParameterDefinition(
    string Name,
    string Label,
    ParameterTier Tier,
    Unit Unit,
    Quantity? Default,
    Quantity? Minimum,
    Quantity? Maximum,
    StandardSeries? Series,
    ParameterKind Kind = ParameterKind.Quantity)
{
    public bool IsRequired => Default is not null;

    public bool IsWaveform => Kind == ParameterKind.Waveform;
}
