using System.Xml.Linq;

namespace Nodal.Architecture.Tests;

internal static class ProjectFile
{
    public static IReadOnlyCollection<string> ProjectReferenceNames(string projectFile) =>
        IncludeAttributes(projectFile, "ProjectReference")
            .Select(ToProjectName)
            .ToArray();

    public static IReadOnlyCollection<string> PackageReferenceNames(string projectFile) =>
        IncludeAttributes(projectFile, "PackageReference").ToArray();

    public static string PropertyValue(string projectFile, string propertyName)
    {
        var value = XDocument.Load(projectFile)
            .Descendants(propertyName)
            .Select(element => element.Value.Trim())
            .FirstOrDefault(candidate => candidate.Length > 0);

        return value ?? throw new InvalidOperationException(
            $"No non-empty {propertyName} property was found in {projectFile}.");
    }

    private static IEnumerable<string> IncludeAttributes(string projectFile, string elementName) =>
        XDocument.Load(projectFile)
            .Descendants(elementName)
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(include => include.Length > 0);

    private static string ToProjectName(string include) =>
        Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar));
}
