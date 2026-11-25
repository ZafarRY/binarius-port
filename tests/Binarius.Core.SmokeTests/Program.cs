
using Binarius.Core.Models;
using Binarius.Core.Services;

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

Console.WriteLine("Binarius.Core smoke tests passed.");
