using Nodal.Content.Diagnostics;

namespace Nodal.Content.Sources;

public interface IContentSource
{
    IReadOnlyList<ContentDiagnostic> Diagnostics => [];

    IEnumerable<ContentFile> ReadAll();
}
