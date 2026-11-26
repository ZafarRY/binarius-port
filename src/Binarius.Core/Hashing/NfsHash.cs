using System.Buffers.Binary;

namespace Binarius.Core.Hashing;

/// <summary>
/// Port of Nikki.Utils.Hashing (BinHash / VltHash) and CoreExtensions Reverse.
/// </summary>
public static class NfsHash
{
    /// <summary>Kept so ToolUi compiles unchanged; the algorithms now come from the original source.</summary>
    public static readonly bool IsVerified = true;

    private const string NullName = "NULL";

    public static uint BinHash(string? value)
    {
        if (TryShortCircuit(value, out var special)) return special;

        var result = uint.MaxValue;
        foreach (var c in value!)
            result = unchecked(result * 0x21 + (byte)c);

        return result;
    }

    public static uint VltHash(string? value)
    {
        if (TryShortCircuit(value, out var special)) return special;

        var bytes = new byte[value!.Length];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)value[i];
        return Lookup2(bytes, 0xABCDEF00u);
    }

    /// <summary>Swaps the byte order (memory hash to file hash and back).</summary>
    public static uint Reverse(uint value) => BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// True when the original hashing returns early: blank text or "NULL" (result 0),
    /// or a "0x..." literal (result is the literal's own value).
    /// </summary>
    public static bool TryShortCircuit(string? value, out uint result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value == NullName) return true;
        return TryParseHexLiteral(value, out result);
    }

    // Same arithmetic as CoreExtensions.TryHexStringToUInt32, including its shift behavior.
    private static bool TryParseHexLiteral(string value, out uint result)
    {
        result = 0;
        if (value.Length < 3) return false;
        if (value[0] != '0') return false;
        if (value[1] != 'x' && value[1] != 'X') return false;

        uint acc = 0;
        for (int i = 2, k = value.Length - 3; i < value.Length; i++, k--)
        {
            var c = value[i];
            int digit;
            if ('0' <= c && c <= '9') digit = c - '0';
            else if ('A' <= c && c <= 'F') digit = c - 'A' + 10;
            else if ('a' <= c && c <= 'f') digit = c - 'a' + 10;
            else return false;

            var mult = unchecked((uint)(1 << (k << 2)));
            acc = unchecked(acc + (uint)digit * mult);
        }

        result = acc;
        return true;
    }

    private static uint Lookup2(byte[] arr, uint seed)
    {
        unchecked
        {
            uint a = 0x9E3779B9, b = 0x9E3779B9, c = seed;
            var v1 = 0;
            var v2 = arr.Length;

            while (v2 >= 12)
            {
                a += BitConverter.ToUInt32(arr, v1);
                b += BitConverter.ToUInt32(arr, v1 + 4);
                c += BitConverter.ToUInt32(arr, v1 + 8);
                Mix(ref a, ref b, ref c);
                v1 += 12;
                v2 -= 12;
            }

            c += (uint)arr.Length;

            if (v2 >= 11) c += (uint)arr[10 + v1] << 24;
            if (v2 >= 10) c += (uint)arr[9 + v1] << 16;
            if (v2 >= 9) c += (uint)arr[8 + v1] << 8;
            if (v2 >= 8) b += (uint)arr[7 + v1] << 24;
            if (v2 >= 7) b += (uint)arr[6 + v1] << 16;
            if (v2 >= 6) b += (uint)arr[5 + v1] << 8;
            if (v2 >= 5) b += arr[4 + v1];
            if (v2 >= 4) a += (uint)arr[3 + v1] << 24;
            if (v2 >= 3) a += (uint)arr[2 + v1] << 16;
            if (v2 >= 2) a += (uint)arr[1 + v1] << 8;
            if (v2 >= 1) a += arr[v1];

            Mix(ref a, ref b, ref c);
            return c;
        }
    }

    // Mix32_1 / Mix32_2 of the original (the final step is the same assignment).
    private static void Mix(ref uint a, ref uint b, ref uint c)
    {
        unchecked
        {
            a = (c >> 13) ^ (a - b - c);
            b = (a << 8) ^ (b - c - a);
            c = (b >> 13) ^ (c - a - b);
            a = (c >> 12) ^ (a - b - c);
            b = (a << 16) ^ (b - c - a);
            c = (b >> 5) ^ (c - a - b);
            a = (c >> 3) ^ (a - b - c);
            b = (a << 10) ^ (b - c - a);
            c = (b >> 15) ^ (c - a - b);
        }
    }
}