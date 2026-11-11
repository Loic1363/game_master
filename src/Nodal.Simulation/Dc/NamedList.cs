namespace Nodal.Simulation.Dc;

internal static class NamedList
{
    public static string Of(IEnumerable<string> names)
    {
        var all = names.ToArray();

        return all.Length <= 1
            ? string.Concat(all)
            : string.Join(", ", all[..^1]) + " and " + all[^1];
    }
}
