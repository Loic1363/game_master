using System.Diagnostics.CodeAnalysis;
using Nodal.Foundation.Schedules;
using Nodal.Foundation.Waveforms;

namespace Nodal.Foundation.Quantities;

public readonly struct ParameterValue : IEquatable<ParameterValue>
{
    private const string NotAQuantity = "This parameter value holds no quantity.";
    private const string NotAWaveform = "This parameter value holds no waveform.";
    private const string NotASchedule = "This parameter value holds no schedule.";

    private readonly Quantity _quantity;
    private readonly Waveform? _waveform;
    private readonly PressSchedule? _schedule;

    public ParameterValue(Quantity quantity)
    {
        _quantity = quantity;
        _waveform = null;
        _schedule = null;
        Unit = quantity.Unit;
    }

    public ParameterValue(Waveform waveform, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        _quantity = default;
        _waveform = waveform;
        _schedule = null;
        Unit = unit;
    }

    public ParameterValue(PressSchedule schedule, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        _quantity = default;
        _waveform = null;
        _schedule = schedule;
        Unit = unit;
    }

    public ParameterKind Kind =>
        _waveform is not null
            ? ParameterKind.Waveform
            : _schedule is not null ? ParameterKind.Schedule : ParameterKind.Quantity;

    public Unit Unit { get; }

    public bool IsQuantity => _waveform is null && _schedule is null;

    public bool IsWaveform => _waveform is not null;

    public bool IsSchedule => _schedule is not null;

    public Quantity Quantity =>
        IsQuantity ? _quantity : throw new InvalidOperationException(NotAQuantity);

    public Waveform Waveform => _waveform ?? throw new InvalidOperationException(NotAWaveform);

    public PressSchedule Schedule =>
        _schedule ?? throw new InvalidOperationException(NotASchedule);

    public static ParameterValue Of(Quantity quantity) => new(quantity);

    public static ParameterValue Of(Waveform waveform, Unit unit) => new(waveform, unit);

    public static ParameterValue Of(PressSchedule schedule, Unit unit) => new(schedule, unit);

    public static implicit operator ParameterValue(Quantity quantity) => new(quantity);

    public bool TryGetQuantity(out Quantity quantity)
    {
        quantity = _quantity;

        return IsQuantity;
    }

    public bool TryGetWaveform([NotNullWhen(true)] out Waveform? waveform)
    {
        waveform = _waveform;

        return _waveform is not null;
    }

    public bool TryGetSchedule([NotNullWhen(true)] out PressSchedule? schedule)
    {
        schedule = _schedule;

        return _schedule is not null;
    }

    public bool Equals(ParameterValue other) =>
        Unit == other.Unit
        && Equals(_waveform, other._waveform)
        && Equals(_schedule, other._schedule)
        && (!IsQuantity || _quantity.Equals(other._quantity));

    public override bool Equals(object? obj) => obj is ParameterValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Unit, _quantity, _waveform, _schedule);

    public static bool operator ==(ParameterValue left, ParameterValue right) => left.Equals(right);

    public static bool operator !=(ParameterValue left, ParameterValue right) => !left.Equals(right);

    public override string ToString() =>
        _waveform?.ToString() ?? _schedule?.ToString() ?? _quantity.ToString();
}
