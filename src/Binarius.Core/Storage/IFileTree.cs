namespace Binarius.Core.Storage;

public interface IFileTree
{
    IReadOnlyList<string> ListFiles();
    string? Resolve(string relativePath);   // casing asli, atau null
    Stream OpenRead(string relativePath);
    void Write(string relativePath, Stream content);
    void Delete(string relativePath);
}

public sealed class LocalFileTree : IFileTree
{
    private readonly string _root;
    public LocalFileTree(string root) => _root = root;

    public IReadOnlyList<string> ListFiles() =>
        Directory.Exists(_root)
            ? Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(_root, p).Replace('\\', '/')).ToList()
            : new List<string>();

    public string? Resolve(string relativePath)
    {
        var current = _root;
        var actual = new List<string>();
        foreach (var segment in relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Directory.EnumerateFileSystemEntries(current)
                .Select(Path.GetFileName)
                .FirstOrDefault(n => string.Equals(n, segment, StringComparison.OrdinalIgnoreCase));
            if (match is null) return null;
            actual.Add(match);
            current = Path.Combine(current, match);
        }
        return actual.Count == 0 ? null : string.Join('/', actual);
    }

    public Stream OpenRead(string relativePath) => File.OpenRead(Path.Combine(_root, relativePath));

    public void Write(string relativePath, Stream content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = File.Create(path);
        content.CopyTo(output);
    }

    public void Delete(string relativePath)
    {
        var path = Path.Combine(_root, relativePath);
        if (File.Exists(path)) File.Delete(path);
    }
}