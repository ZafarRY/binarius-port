using System.Text;
using Binarius.Core.Models;

namespace Binarius.Core.Services;

/// <summary>
/// Reads safe, non-mutating metadata from a selected file. This is an initial
/// Android foundation, not yet a BIN/BUN/LZC parser.
/// </summary>
public sealed class FileInspector
{
    private const int HeaderLength = 64;

    public async Task<OpenedFileInfo> InspectAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        long? length = null;
        if (stream.CanSeek)
        {
            try { length = stream.Length; }
            catch (NotSupportedException) { /* Stream does not expose a length. */ }
        }

        var header = new byte[HeaderLength];
        var count = 0;
        while (count < header.Length)
        {
            var read = await stream.ReadAsync(
                header.AsMemory(count, header.Length - count), cancellationToken);
            if (read == 0) break;
            count += read;
        }

        var bytes = header.AsSpan(0, count).ToArray();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return new OpenedFileInfo(
            Path.GetFileName(fileName),
            extension,
            DetectKind(extension),
            length,
            Convert.ToHexString(bytes),
            ToPrintableAscii(bytes));
    }

    public static SupportedFileKind DetectKind(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".bin" => SupportedFileKind.Bin,
            ".bun" => SupportedFileKind.Bun,
            ".lzc" => SupportedFileKind.Lzc,
            ".end" => SupportedFileKind.Endscript,
            _ => SupportedFileKind.Unknown
        };

    private static string ToPrintableAscii(IEnumerable<byte> bytes)
    {
        var text = Encoding.ASCII.GetString(bytes.ToArray());
        return new string(text.Select(character =>
            char.IsControl(character) ? '.' : character).ToArray());
    }
}
