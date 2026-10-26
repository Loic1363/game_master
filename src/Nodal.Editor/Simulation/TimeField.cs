using Nodal.Foundation.Quantities;

namespace Nodal.Editor.Simulation;

public sealed class TimeField
{
    private const string RequiredMessage = "Required.";
    private const string MalformedNumberMessage = "Not a number.";
    private const string UnknownPrefixMessage = "Unknown metric prefix.";
    private const string OutOfRangeMessage = "Out of range.";
    private const string UnreadableMessage = "Not a valid value.";
    private const string WrongUnitMessage = "Expected a time in seconds.";
    private const string NotPositiveMessage = "Must be greater than zero.";

    internal TimeField(string label, bool required, double? seconds)
    {
        Label = label;
        IsRequired = required;

        Fill(seconds);
    }

    public string Label { get; }

    public bool IsRequired { get; }

    public string Text { get; private set; } = string.Empty;

    public double? Seconds { get; private set; }

    public string? Error { get; private set; }

    public bool IsValid => Error is null;

    public bool IsUnset => Error is null && Seconds is null;

    internal void Write(string? text)
    {
        Text = text ?? string.Empty;
        Seconds = null;
        Error = null;

        var trimmed = Text.Trim();

        if (trimmed.Length == 0)
        {
            Error = IsRequired ? RequiredMessage : null;

            return;
        }

        if (!Quantity.TryParse(trimmed, Unit.Second, out var parsed, out var parseError))
        {
            Error = MessageOf(parseError);

            return;
        }

        if (parsed.Value <= 0)
        {
            Error = NotPositiveMessage;

            return;
        }

        Seconds = parsed.Value;
    }

    private void Fill(double? seconds) =>
        Write(seconds is { } value ? new Quantity(value, Unit.Second).ToString() : string.Empty);

    private static string MessageOf(QuantityParseError error) => error switch
    {
        QuantityParseError.MalformedNumber => MalformedNumberMessage,
        QuantityParseError.UnknownPrefix => UnknownPrefixMessage,
        QuantityParseError.WrongUnit => WrongUnitMessage,
        QuantityParseError.OutOfRange => OutOfRangeMessage,
        _ => UnreadableMessage,
    };
}
