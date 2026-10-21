namespace Nodal.Foundation.Quantities;

public enum QuantityParseError
{
    None,
    Empty,
    MalformedNumber,
    UnknownPrefix,
    WrongUnit,
    OutOfRange,
}
