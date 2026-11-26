using System;
using Binarius.Core.Compression;
using Nikki.Reflection.Enum;

namespace Nikki.Utils
{
    public static class Interop
    {
        public static byte[] Decompress(byte[] input)
        {
            if (input == null || input.Length < 0x10) return null;

            var type = BitConverter.ToInt32(input, 0);
            if (!Enum.IsDefined(typeof(LZCompressionType), type)) return input;

            return LzCodec.Decompress(input);
        }

        public static byte[] Compress(byte[] input, LZCompressionType type)
        {
            if (input == null) return null;
            return LzCodec.Compress(input, (int)type);
        }

        public static byte[] Compress(byte[] input, int start, int count, LZCompressionType type)
        {
            if (input == null) return null;
            if (start < 0 || count <= 0) return null;
            if (start + count > input.Length) return null;
            return LzCodec.Compress(new ReadOnlySpan<byte>(input, start, count), (int)type);
        }
    }
}