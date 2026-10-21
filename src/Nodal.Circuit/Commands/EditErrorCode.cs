namespace Nodal.Circuit.Commands;

public enum EditErrorCode
{
    None,
    UnknownDefinition,
    UnknownComponent,
    UnknownWire,
    UnknownParameter,
    UnitMismatch,
    ValueNotInSeries,
    DiagonalWire,
    ZeroLengthWire,
    DuplicateWire,
}
