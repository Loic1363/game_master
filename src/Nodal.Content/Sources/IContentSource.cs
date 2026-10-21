namespace Nodal.Content.Sources;

public interface IContentSource
{
    IEnumerable<ContentFile> ReadAll();
}
