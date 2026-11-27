
using Binarius.Core.Models;
using Binarius.Core.Services;
using Binarius.Core.Compression;
using Binarius.Core.Hashing;
using Binarius.Core.Launchers;
using Binarius.Core.Storage;

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

// End launcher parsing
static bool ThrowsLauncher(Action action)
{
    try { action(); return false; }
    catch (LauncherException) { return true; }
}

const string endSample = """
    [VERSN1]

    {
      "Usage": "User",
      "Game": "Carbon",
      "Directory": "",
      "Endscript": "mod.endscript",
      "Files": [
        "GLOBAL\GLOBALA.BUN",
        "GLOBAL\GLOBALB.LZC"
      ],
      "Links": [
        {
          "LoadType": "Attributes",
          "PathType": "Absolute",
          "File": "GLOBAL\attributes.bin"
        }
      ]
    }
    """;

var parsedEnd = EndLauncherReader.Parse(endSample, "mod.end");
Equal(LaunchUsage.User, parsedEnd.UsageId, "end usage");
Equal(LaunchGame.Carbon, parsedEnd.GameId, "end game");
Equal("mod.endscript", parsedEnd.Endscript, "end endscript");
Equal(2, parsedEnd.Files.Count, "end files count");
Equal("GLOBAL\\GLOBALA.BUN", parsedEnd.Files[0], "end keeps backslashes");
Equal(1, parsedEnd.Links.Count, "end links count");
Equal(LoaderType.Attributes, parsedEnd.Links[0].LType, "end link loader type");
Equal(LinkPathType.Absolute, parsedEnd.Links[0].PType, "end link path type");
parsedEnd.EnsureUserMode();

// .endlauncher: no header, comments and trailing commas allowed, extension case-insensitive
const string launcherSample = """
    // a comment
    {
      "Usage": "User",
      "Game": "MostWanted",
      "Endscript": "mod.endscript",
      "Files": ["GLOBAL\GLOBALB.LZC",],
    }
    """;

var parsedLauncher = EndLauncherReader.Parse(launcherSample, "MOD.ENDLAUNCHER");
Equal(LaunchGame.MostWanted, parsedLauncher.GameId, "endlauncher game");
Equal(1, parsedLauncher.Files.Count, "endlauncher files count");
Equal(0, parsedLauncher.Links.Count, "endlauncher links default to empty");
Equal(NfsGame.MostWanted, parsedLauncher.GameId.ToNfsGame()!.Value, "game mapping");
Equal(true, LaunchGame.None.ToNfsGame() is null, "no mapping for None");

// Rejections
Equal(true, ThrowsLauncher(() => EndLauncherReader.Parse("{ }", "bad.end")), "missing VERSN1 rejected");
Equal(true, ThrowsLauncher(() => EndLauncherReader.Parse("[VERSN1] not json", "bad.end")), "invalid JSON rejected");
Equal(true, ThrowsLauncher(() => EndLauncherReader.Parse("null", "bad.endlauncher")), "empty launcher rejected");

var modderLauncher = EndLauncherReader.Parse("""{ "Usage": "Modder", "Game": "Carbon" }""", "m.endlauncher");
Equal(true, ThrowsLauncher(() => modderLauncher.EnsureUserMode()), "Modder is not User");
var noGameLauncher = EndLauncherReader.Parse("""{ "Usage": "User", "Game": "Nope" }""", "n.endlauncher");
Equal(true, ThrowsLauncher(() => noGameLauncher.EnsureUserMode()), "unknown game rejected");

// Paths and checks
Equal("GLOBAL/GLOBALA.BUN", LauncherPaths.Normalize("GLOBAL\\GLOBALA.BUN"), "path normalize");
var present = new HashSet<string> { "GLOBAL/GLOBALA.BUN", "mod.endscript" };
Equal(true, parsedEnd.EndscriptExists(present.Contains), "endscript exists");
var missing = parsedEnd.MissingFiles(present.Contains);
Equal(1, missing.Count, "missing files count");
Equal("GLOBAL\\GLOBALB.LZC", missing[0], "missing file name");

// ---- Script path fixing (Windows scripts on a case-sensitive FS)
var modIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["binary/_install.end"] = "Binary/_Install.end",
    ["binary"] = "Binary"
};
string? ResolveMod(string path) => modIndex.TryGetValue(path, out var real) ? real : null;

var fixedScript = ScriptPathFixer.Fix(
    "[VERSN2]\r\n" +
    "append \"binary\\_install.end\"\r\n" +
    "import Binary FRONTEND\\FRONTB.LZC STRBlocks Files\\a.bin\r\n" +
    "infobox \"Keep C:\\Games\\Path as text\"\r\n" +
    "// comment with back\\slash\r\n",
    ResolveMod);
Equal(true, fixedScript.Contains("append \"Binary/_Install.end\""), "append path fixed with real casing");
Equal(true, fixedScript.Contains("FRONTEND/FRONTB.LZC"), "game file token normalized");
Equal(true, fixedScript.Contains("Files/a.bin"), "import path normalized");
Equal(true, fixedScript.Contains("C:\\Games\\Path as text"), "infobox text untouched");
Equal(true, fixedScript.Contains("back\\slash"), "comments untouched");
Equal(true, fixedScript.Contains("\r\n"), "line endings kept");
Equal(false, ScriptPathFixer.IsScript("x.end", "[VERSN1]\n{}"), "launcher is not a script");
Equal(true, ScriptPathFixer.IsScript("x.end", "[VERSN2]\nappend a"), "VERSN2 is a script");

// ---- Staging and write-back
var tmp = Path.Combine(Path.GetTempPath(), "binarius-smoke-" + Guid.NewGuid().ToString("N"));
try
{
    var modRoot = Path.Combine(tmp, "modsrc");
    var gameRoot = Path.Combine(tmp, "gamesrc");
    Directory.CreateDirectory(Path.Combine(modRoot, "Binary"));
    Directory.CreateDirectory(Path.Combine(gameRoot, "FrontEnd"));
    Directory.CreateDirectory(Path.Combine(gameRoot, "Languages"));
    File.WriteAllText(Path.Combine(modRoot, "Binary", "_Install.end"), "[VERSN2]\nappend sub\\x.end\n");
    File.WriteAllBytes(Path.Combine(gameRoot, "FrontEnd", "FrontB.lzc"), new byte[] { 1, 2, 3 });
    File.WriteAllBytes(Path.Combine(gameRoot, "Languages", "Labels.bin"), new byte[] { 9, 9 });

    var launcher = new EndLauncher
    {
        Game = "Underground2",
        Endscript = "binary\\_install.end",
        Files = { "FRONTEND\\FRONTB.LZC" },
        Links = { new LaunchLink { LoadType = "Labels", PathType = "Absolute", File = "languages\\labels.bin" } }
    };

    var modTree = new LocalFileTree(modRoot);
    var gameTree = new LocalFileTree(gameRoot);
    var work = Path.Combine(tmp, "work");

    var staged = UserModeStager.Stage(launcher, modTree, gameTree, work);
    Equal("Binary/_Install.end", staged.Endscript, "endscript real casing");
    Equal("FRONTEND/FRONTB.LZC", staged.Files[0], "staged file upper-case");
    Equal(true, File.Exists(Path.Combine(staged.GameDir, "FRONTEND", "FRONTB.LZC")), "game file staged");
    Equal(true, File.Exists(Path.Combine(staged.GameDir, "languages", "labels.bin")), "link file staged");
    Equal(true, File.ReadAllText(Path.Combine(staged.ModDir, "Binary", "_Install.end")).Contains("sub/x.end"),
        "staged script normalized");

    // nothing changed -> nothing written
    var untouched = UserModeStager.WriteBack(staged, gameTree);
    Equal(0, untouched.Written, "unchanged files are not written");

    // second run: change one file and add one
    var launcher2 = new EndLauncher
    {
        Game = "Underground2",
        Endscript = "Binary/_Install.end",
        Files = { "FrontEnd/FrontB.lzc" }
    };
    var staged2 = UserModeStager.Stage(launcher2, modTree, gameTree, Path.Combine(tmp, "work2"));

    File.WriteAllBytes(Path.Combine(staged2.GameDir, "FRONTEND", "FRONTB.LZC"), new byte[] { 7, 7, 7, 7 });
    Directory.CreateDirectory(Path.Combine(staged2.GameDir, "NEW"));
    File.WriteAllBytes(Path.Combine(staged2.GameDir, "NEW", "added.bin"), new byte[] { 5 });

    var result = UserModeStager.WriteBack(staged2, gameTree);
    Equal(2, result.Written, "changed + new file written");
    Equal(1, result.Backups, "one backup for the changed file");
    Equal(4, File.ReadAllBytes(Path.Combine(gameRoot, "FrontEnd", "FrontB.lzc")).Length,
        "write-back keeps original casing");
    Equal(3, File.ReadAllBytes(Path.Combine(gameRoot, "FrontEnd", "FrontB.lzc.bacc")).Length,
        "backup holds the original bytes");
    Equal(true, File.Exists(Path.Combine(gameRoot, "NEW", "added.bin")), "new file written");
}
finally
{
    if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
}

Console.WriteLine("Binarius.Core smoke tests passed.");
