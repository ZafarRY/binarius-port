
using Binarius.Core.Models;
using Binarius.Core.Services;
using Binarius.Core.Compression;
using Binarius.Core.Hashing;

static void Equal<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException(
            $"{name}: expected {expected}, got {actual}");
}

// Red
var red = PaintSwatchConverter.Convert(255, 0, 0);
Equal(90, red.PaintSwatch, "red paint swatch");
Equal(1f, red.Saturation, "red saturation");
Equal(1f, red.Brightness, "red brightness");

// Green
var green = PaintSwatchConverter.Convert(0, 255, 0);
Equal(60, green.PaintSwatch, "green paint swatch");

// Blue
var blue = PaintSwatchConverter.Convert(0, 0, 255);
Equal(30, blue.PaintSwatch, "blue paint swatch");

// Black
var black = PaintSwatchConverter.Convert(0, 0, 0);
Equal(90, black.PaintSwatch, "black paint swatch");
Equal(0f, black.Saturation, "black saturation");
Equal(0f, black.Brightness, "black brightness");

// White
var white = PaintSwatchConverter.Convert(255, 255, 255);
Equal(90, white.PaintSwatch, "white paint swatch");
Equal(0f, white.Saturation, "white saturation");
Equal(1f, white.Brightness, "white brightness");

// Primary color combinations
Equal(75, PaintSwatchConverter.Convert(255, 255, 0).PaintSwatch,
    "yellow paint swatch");

Equal(45, PaintSwatchConverter.Convert(0, 255, 255).PaintSwatch,
    "cyan paint swatch");

Equal(15, PaintSwatchConverter.Convert(255, 0, 255).PaintSwatch,
    "magenta paint swatch");

// File extension detection
Equal(SupportedFileKind.Bin,
    FileInspector.DetectKind(".BIN"), "BIN extension");

Equal(SupportedFileKind.Bun,
    FileInspector.DetectKind(".bun"), "BUN extension");

Equal(SupportedFileKind.Lzc,
    FileInspector.DetectKind(".lzc"), "LZC extension");

Equal(SupportedFileKind.Endscript,
    FileInspector.DetectKind(".end"), "END extension");

Equal(SupportedFileKind.Unknown,
    FileInspector.DetectKind(".zip"), "unknown extension");

// File inspection
var sample = new byte[] { 0x42, 0x49, 0x4E, 0x00, 0xFF };

await using var stream = new MemoryStream(sample);

var info = await new FileInspector().InspectAsync(
    stream, "sample.bin");

Equal("sample.bin", info.Name, "file name");
Equal(5L, info.Length!.Value, "file length");
Equal("42494E00FF", info.HeaderHex, "hex preview");
Equal("BIN.?", info.HeaderText, "ASCII preview");

// LZ codec: golden vectors
Equal("52415757011000000300000013000000010203",
    Convert.ToHexString(LzCodec.CompressRaww(new byte[] { 1, 2, 3 })), "RAWW golden");

Equal("abcabcabcabc", System.Text.Encoding.ASCII.GetString(LzCodec.Decompress(
    Convert.FromHexString("4A444C5A021000000C0000001700000008016162630206"))), "JDLZ golden");

Equal("ABCDABCDABCD", System.Text.Encoding.ASCII.GetString(LzCodec.Decompress(
    Convert.FromHexString("5246504B000000000C0000001D00000000FB00000CE0414243441403FC"))), "RFPK golden");

Equal("abcabcabc", System.Text.Encoding.ASCII.GetString(LzCodec.Decompress(
    Convert.FromHexString("434F4D5000000000090000001700000008006162630303"))), "COMP golden");

// LZ codec: round trips
var lzRng = new Random(1234);
foreach (var lzSize in new[] { 0, 1, 2, 3, 17, 255, 4096, 70000 })
{
    var randomData = new byte[lzSize];
    lzRng.NextBytes(randomData);
    var repeatData = new byte[lzSize];
    for (var i = 0; i < lzSize; i++) repeatData[i] = (byte)(i % 7);

    foreach (var lzData in new[] { randomData, repeatData })
    {
        Equal(true, lzData.AsSpan().SequenceEqual(
            LzCodec.Decompress(LzCodec.CompressJdlz(lzData))), $"JDLZ round trip {lzSize}");
        Equal(true, lzData.AsSpan().SequenceEqual(
            LzCodec.Decompress(LzCodec.CompressRaww(lzData))), $"RAWW round trip {lzSize}");
        Equal(true, lzData.AsSpan().SequenceEqual(
            LzCodec.Decompress(LzCodec.Compress(lzData, LzCodec.TypeBest))), $"BEST round trip {lzSize}");
    }
}

// Hash helpers
Equal(0x44332211u, NfsHash.Reverse(0x11223344u), "Reverse");
Equal(0x11223344u, NfsHash.Reverse(NfsHash.Reverse(0x11223344u)), "Reverse twice");

// BinHash against real name/hash pairs from Nikki's Map.cs
Equal(0x6212682Bu, NfsHash.BinHash("NUMREMAPCOLOURS"), "BinHash NUMREMAPCOLOURS");
Equal(0x2850A03Bu, NfsHash.BinHash("MORPHTARGET_NUM"), "BinHash MORPHTARGET_NUM");
Equal(0x927097F6u, NfsHash.BinHash("PART_NAME_SELECTOR"), "BinHash PART_NAME_SELECTOR");
Equal(0x643DABEBu, NfsHash.BinHash("LOD_NAME_PREFIX_SELECTOR"), "BinHash LOD_NAME_PREFIX_SELECTOR");
Equal(0x000004B8u, NfsHash.BinHash("CV"), "BinHash CV");

// Special cases of the original
Equal(0u, NfsHash.BinHash(""), "BinHash empty");
Equal(0u, NfsHash.BinHash("   "), "BinHash blank");
Equal(0u, NfsHash.BinHash("NULL"), "BinHash NULL");
Equal(0x1234ABCDu, NfsHash.BinHash("0x1234ABCD"), "BinHash hex literal");
Equal(0u, NfsHash.VltHash(""), "VltHash empty");
Equal(0xABCD1234u, NfsHash.VltHash("0xABCD1234"), "VltHash hex literal");

// VltHash regression values (from an independent port of the original source)
Equal(0x8F3F20AAu, NfsHash.VltHash("CARS"), "VltHash CARS");
Equal(0x4A97EC8Fu, NfsHash.VltHash("pvehicle"), "VltHash pvehicle");
Equal(0xF9382C7Eu, NfsHash.VltHash("ABCDEFGHIJKL"), "VltHash 12 chars");
Equal(0x2EFA68FFu, NfsHash.VltHash("Four score and seven years ago"), "VltHash long");

// Raider hex parsing
uint hexKey;
bool hexTooLong;
Equal(true, HexKey.TryParse("1A2B", out hexKey, out hexTooLong), "hex without prefix");
Equal(0x1A2Bu, hexKey, "hex without prefix value");
Equal(true, HexKey.TryParse("0xDEADBEEF", out hexKey, out hexTooLong), "hex with prefix");
Equal(0xDEADBEEFu, hexKey, "hex with prefix value");
Equal(false, hexTooLong, "hex not too long");
Equal(true, HexKey.TryParse("0x123456789", out hexKey, out hexTooLong), "hex too long parses");
Equal(true, hexTooLong, "hex too long flag");
Equal(false, HexKey.TryParse("0XDEAD", out hexKey, out hexTooLong), "uppercase 0X rejected");
Equal(false, HexKey.TryParse("xyz", out hexKey, out hexTooLong), "non-hex rejected");
Equal(false, HexKey.TryParse("", out hexKey, out hexTooLong), "empty rejected");

// KeyBook
var baseBook = new KeyBook(Array.Empty<string>());
Equal(true, baseBook.TryGetValue(0, out var zeroName), "KeyBook has key 0");
Equal("", zeroName, "KeyBook key 0 is empty");
Equal(true, baseBook.TryGetValue(0x6212682Bu, out var baseName), "KeyBook base key");
Equal("NUMREMAPCOLOURS", baseName, "KeyBook base key name");

var bookOne = new KeyBook(new[] { "// comment", "# comment", "", "NULL", "0x1234", "ZZ_TEST_ONE" });
Equal(baseBook.Count + 1, bookOne.Count, "KeyBook skips comments, blank, NULL and hex lines");
Equal(true, bookOne.TryGetValue(NfsHash.BinHash("ZZ_TEST_ONE"), out var oneName), "KeyBook hit");
Equal("ZZ_TEST_ONE", oneName, "KeyBook value");
Equal(false, bookOne.TryGetValue(0x13579BDFu, out _), "KeyBook miss");

var mergedBook = KeyBook.Merge(new[] { bookOne, new KeyBook(new[] { "ZZ_TEST_TWO" }) });
Equal(baseBook.Count + 2, mergedBook.Count, "KeyBook merge count");

Console.WriteLine("Binarius.Core smoke tests passed.");
