using System.Reflection;

namespace Nodal.Architecture.Tests;

internal static class CoreAssemblies
{
    public static IReadOnlyCollection<string> ReferencedAssemblyNames(string assemblyName)
    {
        var assemblyFile = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");

        if (!File.Exists(assemblyFile))
        {
            throw new InvalidOperationException(
                $"{assemblyName}.dll was not copied next to the test assembly at {AppContext.BaseDirectory}.");
        }

        return Assembly.LoadFrom(assemblyFile)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToArray();
    }
}
