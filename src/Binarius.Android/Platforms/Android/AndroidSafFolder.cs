
using Android.Content;
using Android.Provider;
using AndroidUri = global::Android.Net.Uri;
using Binarius.Core.Storage;

namespace Binarius.Android;

public sealed class AndroidSafFolder : IFileTree
{
    private readonly ContentResolver _resolver;
    private readonly AndroidUri _treeUri;

    public AndroidSafFolder(AndroidUri treeUri)
    {
        _treeUri = treeUri;

        _resolver = Platform.CurrentActivity?.ContentResolver
            ?? throw new InvalidOperationException(
                "ContentResolver unavailable.");
    }

    public async Task<string> GetDisplayNameAsync()
    {
        return await Task.Run(() =>
        {
            var documentId =
                DocumentsContract.GetTreeDocumentId(_treeUri);

            var documentUri =
                DocumentsContract.BuildDocumentUriUsingTree(
                    _treeUri, documentId);
            
            if (documentUri is null)
                return "Unknown folder";
            
            using var cursor = _resolver.Query(
                documentUri,
                new[]
                {
                    DocumentsContract.Document.ColumnDisplayName
                },
                null, null, null);

            if (cursor is null || !cursor.MoveToFirst())
                return "Unknown folder";

            return cursor.GetString(0) ?? "Unknown folder";
        });
    }

    public async Task<bool> ExistsAsync(string relativePath)
    {
        return await Task.Run(() =>
        {
            var normalized = (relativePath ?? "")
                .Replace('\\', '/')
                .Trim('/');

            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            var segments = normalized.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

            if (segments.Any(s => s == "." || s == ".."))
                return false;

            var parentId =
                DocumentsContract.GetTreeDocumentId(_treeUri);

            foreach (var segment in segments)
            {
                var childrenUri =
                    DocumentsContract.BuildChildDocumentsUriUsingTree(
                        _treeUri, parentId);
                
                if (childrenUri is null)
                    return false;
                
                using var cursor = _resolver.Query(
                    childrenUri,
                    new[]
                    {
                        DocumentsContract.Document.ColumnDocumentId,
                        DocumentsContract.Document.ColumnDisplayName
                    },
                    null, null, null);

                if (cursor is null)
                    return false;

                string? foundId = null;

                while (cursor.MoveToNext())
                {
                    var id = cursor.GetString(0);
                    var name = cursor.GetString(1);

                    if (string.Equals(
                        name,
                        segment,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        foundId = id;
                        break;
                    }
                }

                if (foundId is null)
                    return false;

                parentId = foundId;
            }

            return true;
        });
    }

    private sealed record Node(string Id, string Name, bool IsDir);
    private readonly Dictionary<string, List<Node>> _children = new();

    private string RootId => DocumentsContract.GetTreeDocumentId(_treeUri)!;

    private AndroidUri DocUri(string id) =>
        DocumentsContract.BuildDocumentUriUsingTree(_treeUri, id)
        ?? throw new InvalidOperationException("Invalid document id.");

    private List<Node> Children(string parentId)
    {
        if (_children.TryGetValue(parentId, out var cached)) return cached;

        var list = new List<Node>();
        var uri = DocumentsContract.BuildChildDocumentsUriUsingTree(_treeUri, parentId);
        if (uri is not null)
        {
            using var cursor = _resolver.Query(uri, new[]
            {
                DocumentsContract.Document.ColumnDocumentId,
                DocumentsContract.Document.ColumnDisplayName,
                DocumentsContract.Document.ColumnMimeType
            }, null, null, null);

            while (cursor is not null && cursor.MoveToNext())
            {
                var id = cursor.GetString(0);
                var name = cursor.GetString(1);
                var mime = cursor.GetString(2);
                if (id is null || name is null) continue;
                list.Add(new Node(id, name, mime == DocumentsContract.Document.MimeTypeDir));
            }
        }

        _children[parentId] = list;
        return list;
    }

    private Node? Find(string parentId, string name) =>
        Children(parentId).FirstOrDefault(
            n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string[] Split(string path) =>
        path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

    public IReadOnlyList<string> ListFiles()
    {
        var result = new List<string>();
        var pending = new Stack<(string Id, string Prefix)>();
        pending.Push((RootId, string.Empty));

        while (pending.Count > 0)
        {
            var (id, prefix) = pending.Pop();
            foreach (var node in Children(id))
            {
                var path = prefix + node.Name;
                if (node.IsDir) pending.Push((node.Id, path + "/"));
                else result.Add(path);
            }
        }
        return result;
    }

    public string? Resolve(string relativePath)
    {
        var parent = RootId;
        var actual = new List<string>();
        foreach (var segment in Split(relativePath))
        {
            if (segment is "." or "..") return null;
            var node = Find(parent, segment);
            if (node is null) return null;
            actual.Add(node.Name);
            parent = node.Id;
        }
        return actual.Count == 0 ? null : string.Join('/', actual);
    }

    private Node? FindNode(string relativePath)
    {
        var parent = RootId;
        Node? node = null;
        foreach (var segment in Split(relativePath))
        {
            node = Find(parent, segment);
            if (node is null) return null;
            parent = node.Id;
        }
        return node;
    }

    public Stream OpenRead(string relativePath)
    {
        var node = FindNode(relativePath)
            ?? throw new FileNotFoundException("Not found in the selected folder.", relativePath);
        return _resolver.OpenInputStream(DocUri(node.Id))
            ?? throw new IOException($"Could not open {relativePath}.");
    }

    public void Write(string relativePath, Stream content)
    {
        var segments = Split(relativePath);
        if (segments.Length == 0) throw new ArgumentException("Empty path.", nameof(relativePath));

        var parent = RootId;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var folder = Find(parent, segments[i]);
            if (folder is null)
            {
                _ = DocumentsContract.CreateDocument(
                    _resolver, DocUri(parent), DocumentsContract.Document.MimeTypeDir, segments[i])
                    ?? throw new IOException($"Could not create folder {segments[i]}.");
                _children.Remove(parent);
                folder = Find(parent, segments[i])
                    ?? throw new IOException($"Folder {segments[i]} was not created.");
            }
            parent = folder.Id;
        }

        var name = segments[^1];
        var existing = Find(parent, name);
        AndroidUri target;

        if (existing is not null) target = DocUri(existing.Id);
        else
        {
            target = DocumentsContract.CreateDocument(
                _resolver, DocUri(parent), "application/octet-stream", name)
                ?? throw new IOException($"Could not create {name}.");
            _children.Remove(parent);
        }

        using var output = _resolver.OpenOutputStream(target, "wt")
            ?? throw new IOException($"Could not write {name}.");
        content.CopyTo(output);
        output.Flush();
    }

    public void Delete(string relativePath)
    {
        var node = FindNode(relativePath);
        if (node is null) return;
        DocumentsContract.DeleteDocument(_resolver, DocUri(node.Id));
        _children.Clear();
    }
}
