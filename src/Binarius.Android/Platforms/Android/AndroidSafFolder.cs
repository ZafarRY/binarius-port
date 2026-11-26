
using Android.Content;
using Android.Provider;
using AndroidUri = global::Android.Net.Uri;

namespace Binarius.Android;

public sealed class AndroidSafFolder
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
}
