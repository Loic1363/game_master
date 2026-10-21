using Nodal.Content.Sources;

namespace Nodal.Content.Tests;

internal sealed class InMemoryContentSource : IContentSource
{
    private readonly IReadOnlyList<ContentFile> _files;

    public InMemoryContentSource(params ContentFile[] files)
    {
        _files = files;
    }

    public IEnumerable<ContentFile> ReadAll() => _files;
}
