using System.Globalization;

namespace Binarius.Core.Services;

/// <summary>Hex input parsing used by NFS-Raider.</summary>
public static class HexKey
{
    /// <summary>
    /// Mirrors the original: a missing "0x" prefix is added, the rest must be hex digits.
    /// Returns false for invalid input. When valid but longer than 0x + 8 digits,
    /// returns true with <paramref name="tooLong"/> set (the UI shows "N/A").
    /// </summary>
    public static bool TryParse(string? text, out uint key, out bool tooLong)
    {
        key = 0;
        tooLong = false;

        if (string.IsNullOrEmpty(text)) return false;

        var value = text.StartsWith("0x", StringComparison.Ordinal) ? text : "0x" + text;
        if (value.Length <= 2) return false;

        for (var i = 2; i < value.Length; i++)
            if (!Uri.IsHexDigit(value[i])) return false;

        if (value.Length > 10)
        {
            tooLong = true;
            return true;
        }

        key = uint.Parse(value.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return true;
    }
}