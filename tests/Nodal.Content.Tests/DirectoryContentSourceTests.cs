using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Nodal.Content.Symbols;
using Xunit;

namespace Nodal.Content.Tests;

public class DirectoryContentSourceTests
{
    [Fact]
    public void ReadsOnlyJsonFilesInOrdinalOrder()
    {
        using var directory = new TemporaryDirectory();

        directory.Write("second.json", "{}");
        directory.Write("first.json", "[]");
        directory.Write("notes.txt", "ignored");

        var files = new DirectoryContentSource(directory.Path).ReadAll().ToArray();

        Assert.Equal(["first.json", "second.json"], files.Select(file => file.Name));
        Assert.Equal(["[]", "{}"], files.Select(file => file.Text));
    }

    [Fact]
    public void ReadsAnEmptyDirectoryAsNoFile()
    {
        using var directory = new TemporaryDirectory();

        Assert.Empty(new DirectoryContentSource(directory.Path).ReadAll());
    }

    [Fact]
    public void AMissingDirectoryIsReportedAsADiagnosticInsteadOfThrowing()
    {
        var path = Path.Combine(Path.GetTempPath(), "nodal-absent-" + Guid.NewGuid());
        var source = new DirectoryContentSource(path);

        Assert.Empty(source.ReadAll().ToArray());

        var diagnostic = Assert.Single(source.Diagnostics);

        Assert.Equal(ContentDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.StartsWith(
            $"Could not read content file {path}: ", diagnostic.Message, StringComparison.Ordinal);
        Assert.EndsWith(".", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("..", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingDirectoryReachesTheComponentCatalogAsADiagnostic()
    {
        var path = Path.Combine(Path.GetTempPath(), "nodal-absent-" + Guid.NewGuid());
        var result = ComponentCatalogLoader.Load(new DirectoryContentSource(path));

        Assert.Empty(result.Catalog.Definitions);

        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(ContentDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Could not read content file", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingDirectoryReachesTheSymbolLibraryAsADiagnostic()
    {
        var path = Path.Combine(Path.GetTempPath(), "nodal-absent-" + Guid.NewGuid());

        var result = SymbolLibraryLoader.Load(
            new DirectoryContentSource(path), ComponentCatalog.Empty);

        Assert.Equal(0, result.Library.Count);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Message.StartsWith(
                "Could not read content file", StringComparison.Ordinal));
    }

    [Fact]
    public void AReadableDirectoryReportsNoDiagnostic()
    {
        using var directory = new TemporaryDirectory();

        directory.Write("only.json", "{}");

        var source = new DirectoryContentSource(directory.Path);

        Assert.Single(source.ReadAll().ToArray());
        Assert.Empty(source.Diagnostics);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankDirectoryPathIsARejectedArgument(string directory)
    {
        Assert.Throws<ArgumentException>(() => new DirectoryContentSource(directory));
    }

    [Fact]
    public void NullDirectoryPathIsARejectedArgument()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectoryContentSource(null!));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nodal-content-" + Guid.NewGuid());
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string fileName, string text) =>
            File.WriteAllText(System.IO.Path.Combine(Path, fileName), text);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
