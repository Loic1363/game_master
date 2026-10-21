using Nodal.Content.Sources;
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
    public void MissingDirectoryIsAFailure()
    {
        var source = new DirectoryContentSource(Path.Combine(Path.GetTempPath(), "nodal-absent-" + Guid.NewGuid()));

        Assert.Throws<DirectoryNotFoundException>(() => source.ReadAll().ToArray());
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
