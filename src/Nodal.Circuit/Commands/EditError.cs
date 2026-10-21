namespace Nodal.Circuit.Commands;

public sealed record EditError(EditErrorCode Code, string Message)
{
    public override string ToString() => $"{Code}: {Message}";
}
