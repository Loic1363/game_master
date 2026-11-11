namespace Nodal.Simulation.Dc;

internal sealed record DcControl(string PlusNode, string MinusNode, int SenseElement)
{
    public bool HasSense => SenseElement >= 0;
}
