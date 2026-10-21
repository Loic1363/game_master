using System.Xml.Linq;

namespace Nodal.Foundation.Tests;

internal static class DeclaredVersion
{
    public static string FromDirectoryBuildProps { get; } = Read();

    private static string Read()
    {
        var propsFile = RepositoryPaths.DirectoryBuildPropsFile;
        var version = XDocument.Load(propsFile)
            .Descendants("Version")
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => value.Length > 0);

        return version ?? throw new InvalidOperationException(
            $"No non-empty Version property was found in {propsFile}.");
    }
}
