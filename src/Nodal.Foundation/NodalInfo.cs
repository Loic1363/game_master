using System.Reflection;

namespace Nodal.Foundation;

public static class NodalInfo
{
    public const string Name = "Nodal";

    public static string Version { get; } = ReadInformationalVersion();

    private static string ReadInformationalVersion()
    {
        var informationalVersion = typeof(NodalInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? string.Empty;

        var buildMetadataIndex = informationalVersion.IndexOf('+');
        return buildMetadataIndex < 0
            ? informationalVersion
            : informationalVersion[..buildMetadataIndex];
    }
}
