namespace Nodal.Circuit.Commands;

public enum EditErrorCode
{
    None,
    UnknownDefinition,
    UnknownComponent,
    UnknownWire,
    UnknownParameter,
    UnitMismatch,
    KindMismatch,
    InvalidWaveform,
    InvalidSchedule,
    ValueNotInSeries,
    ValueOutOfBounds,
    ParameterIsRequired,
    DiagonalWire,
    ZeroLengthWire,
    DuplicateWire,
    InvalidAnalysisSettings,
    InvalidProbe,
    TooManyProbes,
}
