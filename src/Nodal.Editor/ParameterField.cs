using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;

namespace Nodal.Editor;

public sealed class ParameterField
{
    private const string RequiredMessage = "Required.";
    private const string MalformedNumberMessage = "Not a number.";
    private const string UnknownPrefixMessage = "Unknown metric prefix.";
    private const string OutOfRangeMessage = "Out of range.";
    private const string UnreadableMessage = "Not a valid value.";
    private const string NoUnitMessage = "Expected a value with no unit.";
    private const double Decade = 10;

    private readonly ParameterValue? _original;

    internal ParameterField(ParameterDefinition definition, ParameterValue? current)
    {
        Definition = definition;
        _original = current;

        Fill(current);
    }

    public ParameterDefinition Definition { get; }

    public string Text { get; private set; } = string.Empty;

    public ParameterValue? Value { get; private set; }

    public string? Error { get; private set; }

    public Quantity? Suggestion { get; private set; }

    public bool IsWaveform => Definition.IsWaveform;

    public bool IsValid => Error is null;

    public bool IsUnset => Error is null && Value is null;

    public bool HasChanged => Error is null && Value != _original;

    public bool IsSteppable => Value is { IsQuantity: true } value && value.Quantity.Value > 0;

    internal void Write(string text)
    {
        Text = text ?? string.Empty;

        Validate();
    }

    internal void ApplySuggestion()
    {
        if (Suggestion is { } suggestion)
        {
            Fill(suggestion);
        }
    }

    internal void Step(StepDirection direction)
    {
        if (Value is not { IsQuantity: true } value || value.Quantity.Value <= 0)
        {
            return;
        }

        Fill(new Quantity(Stepped(value.Quantity.Value, direction), Definition.Unit));
    }

    private double Stepped(double value, StepDirection direction)
    {
        if (Definition.Series is { } series)
        {
            return direction == StepDirection.Up
                ? ESeries.Next(series, value)
                : ESeries.Previous(series, value);
        }

        return direction == StepDirection.Up ? value * Decade : value / Decade;
    }

    private void Fill(ParameterValue? value) => Write(value?.ToString() ?? string.Empty);

    private void Validate()
    {
        Value = null;
        Error = null;
        Suggestion = null;

        var text = Text.Trim();

        if (text.Length == 0)
        {
            Error = Definition.IsRequired ? RequiredMessage : null;

            return;
        }

        if (Definition.IsWaveform)
        {
            ValidateWaveform(text);

            return;
        }

        ValidateQuantity(text);
    }

    private void ValidateWaveform(string text)
    {
        if (WaveformText.TryParse(text, out var waveform, out var error))
        {
            Value = ParameterValue.Of(waveform, Definition.Unit);

            return;
        }

        Error = WaveformErrorText.Of(error);
    }

    private void ValidateQuantity(string text)
    {
        if (!Quantity.TryParse(text, Definition.Unit, out var parsed, out var parseError))
        {
            Error = MessageOf(parseError);

            return;
        }

        if (Definition.Minimum is { } minimum && parsed.Value < minimum.Value)
        {
            Error = $"Must be at least {minimum}.";

            return;
        }

        if (Definition.Maximum is { } maximum && parsed.Value > maximum.Value)
        {
            Error = $"Must be at most {maximum}.";

            return;
        }

        if (Definition.Series is { } series && !BelongsToSeries(series, parsed.Value))
        {
            Error = $"Not in {series}.";

            if (parsed.Value > 0)
            {
                Suggestion = new Quantity(ESeries.Nearest(series, parsed.Value), Definition.Unit);
            }

            return;
        }

        Value = parsed;
    }

    private string MessageOf(QuantityParseError error) => error switch
    {
        QuantityParseError.MalformedNumber => MalformedNumberMessage,
        QuantityParseError.UnknownPrefix => UnknownPrefixMessage,
        QuantityParseError.WrongUnit => WrongUnitMessage(),
        QuantityParseError.OutOfRange => OutOfRangeMessage,
        _ => UnreadableMessage,
    };

    private string WrongUnitMessage()
    {
        var symbol = Definition.Unit.Symbol();

        return symbol.Length == 0 ? NoUnitMessage : $"Expected a value in {symbol}.";
    }

    private static bool BelongsToSeries(StandardSeries series, double value) =>
        value > 0 && ESeries.Contains(series, value);
}
