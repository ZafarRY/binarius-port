using System.Reflection;
using Microsoft.Maui.Storage;

namespace Binarius.Android;

/// <summary>Opens the bundled MainKeys hash lists (embedded first, app package as fallback).</summary>
internal static class KeyAssets
{
    public static async Task<Stream> OpenAsync(string fileName)
    {
        var embedded = typeof(KeyAssets).Assembly.GetManifestResourceStream($"MainKeys/{fileName}");
        if (embedded is not null) return embedded;

        try
        {
            return await FileSystem.OpenAppPackageFileAsync($"LegacyData/MainKeys/{fileName}");
        }
        catch (Exception ex)
        {
            var known = string.Join(", ", typeof(KeyAssets).Assembly.GetManifestResourceNames());
            throw new FileNotFoundException(
                $"Key file '{fileName}' not found in app. Embedded: [{known}]", ex);
        }
    }
}