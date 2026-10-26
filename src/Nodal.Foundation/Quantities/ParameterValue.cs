using System.Diagnostics.CodeAnalysis;
using Nodal.Foundation.Waveforms;

namespace Nodal.Foundation.Quantities;

public readonly struct ParameterValue : IEquatable<ParameterValue>
{
    private const string NotAQuantity = "This parameter value holds a waveform, not a quantity.";
    private const string NotAWaveform = "This parameter value holds a quantity, not a waveform.";

    private readonly Quantity _quantity;
    private readonly Waveform? _waveform;

    public ParameterValue(Quantity quantity)
    {
        _quantity = quantity;
        _waveform = null;
        Unit = quantity.Unit;
    }

    public ParameterValue(Waveform waveform, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        _quantity = default;
        _waveform = waveform;
        Unit = unit;
    }

    public ParameterKind Kind =>
        _waveform is null ? ParameterKind.Quantity : ParameterKind.Waveform;

    public Unit Unit { get; }

    public bool IsQuantity => _waveform is null;

    public bool IsWaveform => _waveform is not null;

    public Quantity Quantity =>
        _waveform is null ? _quantity : throw new InvalidOperationException(NotAQuantity);

    public Waveform Waveform => _waveform ?? throw new InvalidOperationException(NotAWaveform);

    public static ParameterValue Of(Quantity quantity) => new(quantity);

    public static ParameterValue Of(Waveform waveform, Unit unit) => new(waveform, unit);

    public static implicit operator ParameterValue(Quantity quantity) => new(quantity);

    public bool TryGetQuantity(out Quantity quantity)
    {
        quantity = _quantity;

        return _waveform is null;
    }

    public bool TryGetWaveform([NotNullWhen(true)] out Waveform? waveform)
    {
        waveform = _waveform;

        return _waveform is not null;
    }

    public bool Equals(ParameterValue other) =>
        Unit == other.Unit
        && (_waveform is null
            ? other._waveform is null && _quantity.Equals(other._quantity)
            : _waveform.Equals(other._waveform));

    public override bool Equals(object? obj) => obj is ParameterValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Unit, _quantity, _waveform);

    public static bool operator ==(ParameterValue left, ParameterValue right) => left.Equals(right);

    public static bool operator !=(ParameterValue left, ParameterValue right) => !left.Equals(right);

    public override string ToString() => _waveform?.ToString() ?? _quantity.ToString();
}
