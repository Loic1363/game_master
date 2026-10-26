namespace Nodal.Foundation.Waveforms;

public enum WaveformError
{
    None,
    Empty,
    UnknownFunction,
    MalformedSyntax,
    WrongArgumentCount,
    MalformedNumber,
    UnknownPrefix,
    UnitNotAllowed,
    NumberOutOfRange,
    NegativeTime,
    PeriodTooShort,
    NonPositiveTau,
    InvalidCycleCount,
    NoPoints,
    UnorderedPoints,
}
