using Binarius.Core.Hashing;
using Binarius.Core.Models;

namespace Binarius.Core.Services;

/// <summary>
/// Hash to name lookup that mirrors Map.ReloadBinKeys + Loader.LoadBinKeys of Nikki.
/// </summary>
public sealed class KeyBook
{
    // The built-in names added by Map.ReloadBinKeys.
    private static readonly string[] BaseKeys =
    [
        "CV", "RED", "BLUE", "RED2", "MAT0", "MAT1", "MAT2", "MAT3", "MAT4", "MAT5", "MAT6", "MAT7",
        "NAME", "SIZE", "DISPGREEN", "AcidEffects", "ALPHA", "BLEND", "BLUE2", "GREEN", "GLOSS",
        "REMAP", "SHAPE", "STOCK", "TIREHUE", "TIRELUM", "TIRESAT", "DISPBLUE", "USEMARKER1",
        "USEMARKER2", "HOODRIGHT", "HOODUNDER", "TEXTURE_NAME", "MODEL_TABLE_OFFSET", "SlotOverrides",
        "SPOKE_COUNT", "KITNOTPAINT01", "KITNOTPAINT03", "KITNOTPAINT04", "KITNOTPAINT06",
        "KITNOTPAINT07", "KITNOTPAINT10", "KITNOTPAINT18", "GCareers", "MORPHTARGET_NUM",
        "GROUPLANGUAGEHASH", "Collisions", "HUDINDEX", "DISPRED", "GCareerRaces", "TPKBlocks",
        "ANIMSTYLE", "VERTSPLIT", "LOD_CHARACTERS_OFFSET", "LANGUAGEHASH", "FNGroups", "DAMAGELEVEL",
        "AcidEmitters", "ALPHA2", "CENTER", "NUMCOLOURS", "CarTypeInfos", "SunInfos", "Materials",
        "GREEN2", "NUMREMAPCOLOURS", "VINYLLANGUAGEHASH", "LOD_NAME_PREFIX_SELECTOR", "UNPAINTABLE",
        "WHEELLEFT", "FULLBODY", "LIGHT_MATERIAL_NAME", "MIRROR", "STOCK_MATERIAL", "ONLINE",
        "CARBONFIBRE", "DBModelParts", "RANDOM", "HOODHUE", "HOODLUM", "HOODSAT", "KITNUMBER",
        "SWATCH", "HOODLEFT", "WIDEBODYKIT", "NAME_OFFSET", "Tracks", "COMPLEXTARGET", "TEXTUREHASH",
        "SPECIFICCARNAME", "STRBlocks", "PART_NAME_BASE_HASH", "PARTID_UPGRADE_GROUP",
        "PART_NAME_SELECTOR", "SPINNER_TEXTURE", "LOD_NAME_PREFIX_NAMEHASH", "SPINNEROFFSET",
        "VectorVinyls", "NUM_DECALS", "PART_NAME_OFFSETS", "HOODEMITTER", "WHEELRIGHT",
        "LANGUAGEHASHABR", "OUTER_RADIUS", "ISDECAL", "PresetRides", "PresetSkins", "SPEECHCOLOUR",
        "COLOR0ID", "COLOR1ID", "COLOR2ID", "COLOR3ID", "MAX_LOD", "SlotTypes", "EXCLUDEDECAL",
        "INNER_RADIUS", "BRAND_NAME", "PAINTGROUP", "WHEELEMITTER", "MATNAMEA", "MATNAMEB",
        "EXCLUDE_SUV", "EXCLUDE_UG1", "EXCLUDE_UG2", "WARNONDELETE", "TEXTURE", "LOD_BASE_NAME"
    ];

    private readonly Dictionary<uint, string> _keys;

    public KeyBook(IEnumerable<string> lines)
    {
        _keys = new Dictionary<uint, string>
        {
            [0] = string.Empty,
            [0xFFFFFFFF] = string.Empty
        };

        foreach (var key in BaseKeys) Add(key);

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r'); // other whitespace is part of the key, as in the original
            if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('#')) continue;
            Add(line);
        }
    }

    private KeyBook(Dictionary<uint, string> keys) => _keys = keys;

    public int Count => _keys.Count;

    public bool TryGetValue(uint key, out string value) =>
        _keys.TryGetValue(key, out value!);

    public static KeyBook Merge(IEnumerable<KeyBook> books)
    {
        var merged = new Dictionary<uint, string>();
        foreach (var book in books)
            foreach (var pair in book._keys)
                merged.TryAdd(pair.Key, pair.Value);
        return new KeyBook(merged);
    }

    public static string FileName(NfsGame game) => game switch
    {
        NfsGame.Underground1 => "underground1.txt",
        NfsGame.Underground2 => "underground2.txt",
        NfsGame.MostWanted => "mostwanted.txt",
        NfsGame.Carbon => "carbon.txt",
        NfsGame.ProStreet => "prostreet.txt",
        NfsGame.Undercover => "undercover.txt",
        _ => throw new ArgumentOutOfRangeException(nameof(game))
    };

    private void Add(string value)
    {
        if (NfsHash.TryShortCircuit(value, out _)) return; // blank, "NULL", "0x..." are never stored
        _keys.TryAdd(NfsHash.BinHash(value), value);       // first name wins on collisions
    }
}