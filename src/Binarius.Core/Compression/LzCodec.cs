using System.Buffers.Binary;

namespace Binarius.Core.Compression;

/// <summary>
/// Managed replacement for LZCompressLib.dll (BlockCompress / BlockDecompress).
/// Stream layouts were derived from disassembling the original library.
/// </summary>
public static class LzCodec
{
    public const int HeaderSize = 0x10;

    public const int TypeRaww = 0x57574152; // "RAWW"
    public const int TypeJdlz = 0x5A4C444A; // "JDLZ"
    public const int TypeComp = 0x504D4F43; // "COMP"
    public const int TypeRfpk = 0x4B504652; // "RFPK"
    public const int TypeHuff = 0x46465548; // "HUFF"
    public const int TypeBest = 0x54534542; // "BEST"

    private const int MaxOutputSize = 0x40000000;
    private const int JdlzMaxDistance = 0x810;
    private const int JdlzMaxLength = 0x1002;
    private const int JdlzMaxLongDistanceLength = 0x22;
    private const int JdlzMaxChain = 64;

    // ------------------------------------------------------------ decompress

    public static byte[] Decompress(ReadOnlySpan<byte> input)
    {
        if (input.Length < HeaderSize)
            throw new InvalidDataException("Buffer is smaller than the 16-byte LZ header.");

        var type = BinaryPrimitives.ReadInt32LittleEndian(input);
        var outputSize = BinaryPrimitives.ReadInt32LittleEndian(input.Slice(8));
        if (outputSize < 0 || outputSize > MaxOutputSize)
            throw new InvalidDataException($"Invalid decompressed size: {outputSize}.");

        var output = new byte[outputSize];
        switch (type)
        {
            case TypeRaww: DecodeRaww(input, output); break;
            case TypeJdlz: DecodeJdlz(input, output); break;
            case TypeComp: DecodeComp(input, output); break;
            case TypeRfpk: DecodeRfpk(input, output); break;
            case TypeHuff: DecodeHuff(input, output); break;
            default:
                throw new InvalidDataException($"Unsupported LZ type 0x{type:X8}.");
        }
        return output;
    }

    private static void DecodeRaww(ReadOnlySpan<byte> src, byte[] dst)
    {
        var count = Math.Min(src.Length - HeaderSize, dst.Length);
        src.Slice(HeaderSize, count).CopyTo(dst);
    }

    private static void DecodeJdlz(ReadOnlySpan<byte> src, byte[] dst)
    {
        var ip = HeaderSize;
        var op = 0;
        var flag1 = 1;
        var flag2 = 1;

        while (ip < src.Length && op < dst.Length)
        {
            if (flag1 == 1) flag1 = At(src, ip++) | 0x100;
            if (flag2 == 1) flag2 = At(src, ip++) | 0x100;

            if ((flag1 & 1) != 0)
            {
                var b0 = At(src, ip);
                var b1 = At(src, ip + 1);
                int length, distance;
                if ((flag2 & 1) != 0)
                {
                    length = (((b0 & 0xF0) << 4) | b1) + 3;
                    distance = (b0 & 0x0F) + 1;
                }
                else
                {
                    length = (b0 & 0x1F) + 3;
                    distance = (((b0 & 0xE0) << 3) | b1) + 0x11;
                }
                ip += 2;
                CopyMatch(dst, ref op, distance, length);
                flag2 >>= 1;
            }
            else
            {
                dst[op++] = (byte)At(src, ip++);
            }
            flag1 >>= 1;
        }
    }

    private static void DecodeComp(ReadOnlySpan<byte> src, byte[] dst)
    {
        if (src[6] == 1 && src[7] == 0)
        {
            DecodeRaww(src, dst);
            return;
        }

        var ip = HeaderSize;
        var op = 0;
        var flags = 1;
        var limit = src.Length - 0x20;

        while (ip < src.Length && op < dst.Length)
        {
            if (flags == 1)
            {
                if (ip + 1 >= src.Length) break;
                flags = ((src[ip + 1] | 0x100) << 8) | src[ip];
                ip += 2;
            }

            var count = limit < ip ? 1 : 16;
            for (var k = 0; k < count && op < dst.Length && ip < src.Length; k++)
            {
                var b = src[ip];
                if ((flags & 1) != 0)
                {
                    var offset = ((b & 0xF0) << 4) | At(src, ip + 1);
                    ip += 2;
                    CopyMatch(dst, ref op, offset, (b & 0x0F) + 3);
                }
                else
                {
                    dst[op++] = (byte)b;
                    ip++;
                }
                flags >>= 1;
            }
        }
    }

    private static void DecodeRfpk(ReadOnlySpan<byte> src, byte[] dst)
    {
        var ip = HeaderSize;
        var flags = At(src, ip);
        var large = (flags & 0x80) != 0;
        var hasCompressedSize = (flags & 0x01) != 0;
        ip += large
            ? 6 + (hasCompressedSize ? 4 : 0)
            : 5 + (hasCompressedSize ? 3 : 0);

        var op = 0;
        while (ip < src.Length)
        {
            var b0 = At(src, ip);
            if (b0 < 0x80)
            {
                var b1 = At(src, ip + 1);
                ip += 2;
                CopyLiterals(src, ref ip, dst, ref op, b0 & 3);
                CopyMatch(dst, ref op, ((b0 & 0x60) << 3) + b1 + 1, ((b0 >> 2) & 7) + 3);
            }
            else if (b0 < 0xC0)
            {
                var b1 = At(src, ip + 1);
                var b2 = At(src, ip + 2);
                ip += 3;
                CopyLiterals(src, ref ip, dst, ref op, b1 >> 6);
                CopyMatch(dst, ref op, ((b1 & 0x3F) << 8) + b2 + 1, (b0 & 0x3F) + 4);
            }
            else if (b0 < 0xE0)
            {
                var b1 = At(src, ip + 1);
                var b2 = At(src, ip + 2);
                var b3 = At(src, ip + 3);
                ip += 4;
                CopyLiterals(src, ref ip, dst, ref op, b0 & 3);
                CopyMatch(dst, ref op,
                    ((b0 & 0x10) << 12) + (b1 << 8) + b2 + 1,
                    ((b0 & 0x0C) << 6) + b3 + 5);
            }
            else
            {
                var run = ((b0 & 0x1F) << 2) + 4;
                ip += 1;
                if (run <= 0x70)
                {
                    CopyLiterals(src, ref ip, dst, ref op, run);
                }
                else
                {
                    CopyLiterals(src, ref ip, dst, ref op, b0 & 3);
                    break;
                }
            }
        }
    }

    // ------------------------------------------------------------------ HUFF

    /// <summary>MSB-first bit reader; reads past the end return zero bits.</summary>
    private ref struct HuffBits
    {
        private readonly ReadOnlySpan<byte> _s;
        private int _pos;
        private ulong _buf;
        private int _cnt;

        public HuffBits(ReadOnlySpan<byte> s)
        {
            _s = s;
            _pos = 0;
            _buf = 0;
            _cnt = 0;
        }

        public bool Overrun => _pos - (_cnt >> 3) > _s.Length + 8;

        private void Fill()
        {
            while (_cnt <= 56)
            {
                ulong b = (uint)_pos < (uint)_s.Length ? _s[_pos] : 0u;
                _buf |= b << (56 - _cnt);
                _pos++;
                _cnt += 8;
            }
        }

        public uint Peek(int n)
        {
            if (n == 0) return 0;
            if (_cnt < n) Fill();
            return (uint)(_buf >> (64 - n));
        }

        public void Skip(int n)
        {
            if (n == 0) return;
            if (_cnt < n) Fill();
            _buf <<= n;
            _cnt -= n;
        }

        public uint Read(int n)
        {
            var v = Peek(n);
            Skip(n);
            return v;
        }
    }

    /// <summary>Variable-length number: z zeros, a 1, then (z+2) bits v; result v + 2^(z+2) - 4.</summary>
    private static int HuffCount(ref HuffBits br)
    {
        var zeros = 0;
        while (br.Read(1) == 0)
        {
            if (++zeros > 28 || br.Overrun)
                throw new InvalidDataException("Corrupt HUFF stream.");
        }

        var width = zeros + 2;
        return (int)br.Read(width) + (1 << width) - 4;
    }

    private static void DecodeHuff(ReadOnlySpan<byte> src, byte[] dst)
    {
        var br = new HuffBits(src.Slice(HeaderSize));

        // RefPack-like header: flags, optional packed size, unpacked size, escape symbol.
        var flags = (int)br.Read(16);
        var large = (flags & 0x8000) != 0;
        if ((flags & 0x0100) != 0) br.Skip(large ? 32 : 24);
        flags &= ~0x0100;
        var declaredSize = (int)br.Read(large ? 32 : 24);
        var escape = (int)br.Read(8);

        // Canonical code table: number of codes for each bit length.
        var count = new int[18];
        var limit = new int[18];
        var offset = new int[18];
        int code = 0, total = 0, maxLen = 0;

        for (var len = 1; ; len++)
        {
            if (len > 16) throw new InvalidDataException("Invalid HUFF code table.");

            code <<= 1;
            offset[len] = code - total;

            var n = HuffCount(ref br);
            if (n < 0 || total + n > 256)
                throw new InvalidDataException("Invalid HUFF code table.");

            count[len] = n;
            code += n;
            total += n;
            limit[len] = n != 0 ? (code << (16 - len)) & 0xFFFF : 0;
            maxLen = len;

            if (n != 0 && limit[len] == 0) break; // code space is full
        }
        limit[maxLen] = int.MaxValue;

        // Symbols in code order, stored as gaps between unused byte values.
        var symbols = new byte[total];
        var used = new bool[256];
        var prev = -1;
        for (var k = 0; k < total; k++)
        {
            var skip = HuffCount(ref br) + 1;
            var s = prev;
            while (skip > 0)
            {
                if (++s > 255) throw new InvalidDataException("Invalid HUFF symbol table.");
                if (!used[s]) skip--;
            }
            used[s] = true;
            symbols[k] = (byte)s;
            prev = s;
        }

        // 8-bit fast lookup for the short codes.
        var fastSym = new byte[256];
        var fastLen = new byte[256];
        Array.Fill(fastLen, (byte)0x40);
        var escLen = 0;
        var next = 0;
        var pos = 0;
        for (var len = 1; len <= maxLen && len <= 8; len++)
        {
            var span = 1 << (8 - len);
            for (var c = 0; c < count[len]; c++)
            {
                var sym = symbols[next++];
                byte tag = (byte)len;
                if (sym == escape)
                {
                    escLen = len;
                    tag = 0x60;
                }
                if (pos + span > 256) throw new InvalidDataException("Invalid HUFF code table.");
                for (var j = 0; j < span; j++)
                {
                    fastSym[pos + j] = sym;
                    fastLen[pos + j] = tag;
                }
                pos += span;
            }
        }

        // Decode.
        var op = 0;
        while (op < dst.Length)
        {
            if (br.Overrun) throw new InvalidDataException("Compressed stream is truncated.");

            var idx = (int)br.Peek(8);
            var tag = fastLen[idx];
            int symbol;

            if (tag <= 8)
            {
                br.Skip(tag);
                symbol = fastSym[idx];
            }
            else
            {
                int len;
                if (tag == 0x60)
                {
                    len = escLen;
                }
                else
                {
                    if (maxLen < 9) throw new InvalidDataException("Invalid HUFF code.");
                    var top = (int)br.Peek(16);
                    len = 9;
                    while (len < maxLen && top >= limit[len]) len++;
                }

                var bits = (int)br.Peek(len);
                br.Skip(len);
                var index = bits - offset[len];
                if ((uint)index >= (uint)total) throw new InvalidDataException("Invalid HUFF code.");
                symbol = symbols[index];
            }

            if (symbol != escape)
            {
                dst[op++] = (byte)symbol;
                continue;
            }

            // Escape: run of the previous byte, literal byte, or end of stream.
            var run = HuffCount(ref br);
            if (run != 0)
            {
                if (op == 0) throw new InvalidDataException("Invalid HUFF run.");
                var last = dst[op - 1];
                var end = Math.Min(op + run, dst.Length);
                while (op < end) dst[op++] = last;
                continue;
            }

            if (br.Read(1) != 0) break; // end marker
            dst[op++] = (byte)br.Read(8);
        }

        // Optional delta filters.
        var size = declaredSize > 0 ? Math.Min(declaredSize, dst.Length) : dst.Length;
        switch (flags)
        {
            case 0x32FB:
            case 0xB2FB:
            {
                var sum = 0;
                for (var i = 0; i < size; i++)
                {
                    sum += dst[i];
                    dst[i] = (byte)sum;
                }
                break;
            }
            case 0x34FB:
            case 0xB4FB:
            {
                int sum = 0, sum2 = 0;
                for (var i = 0; i < size; i++)
                {
                    sum += dst[i];
                    sum2 += sum;
                    dst[i] = (byte)sum2;
                }
                break;
            }
        }
    }

    private static int At(ReadOnlySpan<byte> s, int i)
    {
        if ((uint)i >= (uint)s.Length)
            throw new InvalidDataException("Compressed stream is truncated.");
        return s[i];
    }

    private static void CopyLiterals(
        ReadOnlySpan<byte> src, ref int ip, byte[] dst, ref int op, int count)
    {
        if (count <= 0) return;
        if (ip + count > src.Length)
            throw new InvalidDataException("Compressed stream is truncated.");
        var n = Math.Min(count, dst.Length - op);
        if (n > 0)
        {
            src.Slice(ip, n).CopyTo(dst.AsSpan(op));
            op += n;
        }
        ip += count;
    }

    private static void CopyMatch(byte[] dst, ref int op, int distance, int length)
    {
        if (distance <= 0 || distance > op)
            throw new InvalidDataException("Match distance points before the start of the output.");
        var from = op - distance;
        var end = Math.Min(op + length, dst.Length);
        while (op < end) dst[op++] = dst[from++]; // byte-wise: overlap is intentional
    }

    // -------------------------------------------------------------- compress

    public static byte[] Compress(ReadOnlySpan<byte> data, int type)
    {
        switch (type)
        {
            case TypeRaww:
                return CompressRaww(data);
            case TypeJdlz:
                return CompressJdlz(data);
            case TypeBest:
            {
                var packed = CompressJdlz(data);
                return packed.Length < data.Length + HeaderSize ? packed : CompressRaww(data);
            }
            case TypeHuff:
                throw new NotSupportedException("HUFF compression is not ported yet.");
            default:
                throw new ArgumentException($"Unsupported compression type 0x{type:X8}.", nameof(type));
        }
    }

    public static byte[] CompressRaww(ReadOnlySpan<byte> data)
    {
        var o = new byte[data.Length + HeaderSize];
        WriteHeader(o, TypeRaww, 0x1001, data.Length, o.Length);
        data.CopyTo(o.AsSpan(HeaderSize));
        return o;
    }

    public static byte[] CompressJdlz(ReadOnlySpan<byte> data)
    {
        var n = data.Length;
        var o = new byte[n + (n >> 3) + 64];
        var op = HeaderSize + 2;
        var f1Pos = HeaderSize;
        var f2Pos = HeaderSize + 1;
        int f1 = 0, f1n = 0, f2 = 0, f2n = 0;

        var head = new int[1 << 15];
        Array.Fill(head, -1);
        var prev = new int[n];

        var ip = 0;
        while (ip < n)
        {
            int bestLen = 0, bestDist = 0;
            if (ip + 2 < n)
            {
                var maxLen = Math.Min(JdlzMaxLength, n - ip);
                var cand = head[Hash(data, ip)];
                for (var chain = 0; cand >= 0 && chain < JdlzMaxChain; chain++)
                {
                    var dist = ip - cand;
                    if (dist > JdlzMaxDistance) break;
                    var cap = dist <= 16 ? maxLen : Math.Min(maxLen, JdlzMaxLongDistanceLength);
                    var l = 0;
                    while (l < cap && data[cand + l] == data[ip + l]) l++;
                    if (l > bestLen)
                    {
                        bestLen = l;
                        bestDist = dist;
                        if (l == maxLen) break;
                    }
                    cand = prev[cand];
                }
            }

            int advance;
            if (bestLen >= 3)
            {
                var shortDistance = bestDist <= 16;
                int b0, b1;
                if (shortDistance)
                {
                    var l3 = bestLen - 3;
                    b0 = ((l3 >> 8) << 4) | (bestDist - 1);
                    b1 = l3 & 0xFF;
                }
                else
                {
                    var d = bestDist - 0x11;
                    b0 = ((d >> 8) << 5) | (bestLen - 3);
                    b1 = d & 0xFF;
                }
                o[op++] = (byte)b0;
                o[op++] = (byte)b1;
                f1 |= 1 << f1n; f1n++;
                if (shortDistance) f2 |= 1 << f2n;
                f2n++;
                advance = bestLen;
            }
            else
            {
                o[op++] = data[ip];
                f1n++;
                advance = 1;
            }

            if (f1n == 8) { o[f1Pos] = (byte)f1; f1 = 0; f1n = 0; f1Pos = op++; }
            if (f2n == 8) { o[f2Pos] = (byte)f2; f2 = 0; f2n = 0; f2Pos = op++; }

            for (var k = 0; k < advance; k++)
            {
                var p = ip + k;
                if (p + 2 < n)
                {
                    var h = Hash(data, p);
                    prev[p] = head[h];
                    head[h] = p;
                }
            }
            ip += advance;
        }

        o[f1Pos] = (byte)f1;
        o[f2Pos] = (byte)f2;
        WriteHeader(o, TypeJdlz, 0x1002, n, op);
        return o.AsSpan(0, op).ToArray();
    }

    private static int Hash(ReadOnlySpan<byte> d, int p) =>
        unchecked(((d[p] << 8) ^ (d[p + 1] << 4) ^ d[p + 2]) * 0x9E37) & 0x7FFF;

    private static void WriteHeader(Span<byte> o, int type, int version, int rawSize, int packedSize)
    {
        BinaryPrimitives.WriteInt32LittleEndian(o, type);
        BinaryPrimitives.WriteInt32LittleEndian(o.Slice(4), version);
        BinaryPrimitives.WriteInt32LittleEndian(o.Slice(8), rawSize);
        BinaryPrimitives.WriteInt32LittleEndian(o.Slice(12), packedSize);
    }
}