using System.Text.Json;
using Binarius.Core.Models;

namespace Binarius.Core.Launchers;

/// <summary>Mirrors Endscript.Enums.eUsage.</summary>
public enum LaunchUsage { Invalid = 0, User = 1, Modder = 2 }

/// <summary>Mirrors Nikki.Core.GameINT (same numeric values).</summary>
public enum LaunchGame
{
    None = 0,
    Carbon = 1,
    MostWanted = 2,
    Underground2 = 3,
    Underground1 = 4,
    Prostreet = 5,
    Undercover = 6
}

/// <summary>Mirrors Endscript.Enums.eLoaderType.</summary>
public enum LoaderType { Invalid = 0, BinKeys = 1, VltKeys = 2, Attributes = 3, FeAttrib = 4, Labels = 5 }

/// <summary>Mirrors Endscript.Enums.ePathType.</summary>
public enum LinkPathType { Invalid = 0, Relative = 1, Absolute = 2 }

public sealed class LauncherException : Exception
{
    public LauncherException(string message) : base(message) { }
    public LauncherException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Mirrors Endscript.Helpers.SubLoader.</summary>
public sealed class LaunchLink
{
    public string LoadType { get; set; } = string.Empty;
    public string PathType { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;

    public LoaderType LType => Enum.TryParse(LoadType, out LoaderType type) ? type : LoaderType.Invalid;
    public LinkPathType PType => Enum.TryParse(PathType, out LinkPathType type) ? type : LinkPathType.Invalid;
}

/// <summary>Mirrors Endscript.Core.Launch (the content of .end / .endlauncher files).</summary>
public sealed class EndLauncher
{
    public string Usage { get; set; } = nameof(LaunchUsage.User);
    public string Game { get; set; } = nameof(LaunchGame.None);
    public string Directory { get; set; } = string.Empty;
    public string Endscript { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
    public List<LaunchLink> Links { get; set; } = new();

    public LaunchUsage UsageId => Enum.TryParse(Usage, out LaunchUsage usage) ? usage : LaunchUsage.Invalid;
    public LaunchGame GameId => Enum.TryParse(Game, out LaunchGame game) ? game : LaunchGame.None;

    /// <summary>Same checks and messages as the desktop User Mode.</summary>
    public void EnsureUserMode()
    {
        if (UsageId != LaunchUsage.User)
            throw new LauncherException(
                $"Usage type of the endscript is stated to be {Usage}, while should be User");

        if (GameId == LaunchGame.None)
            throw new LauncherException($"Invalid stated game type named {Game}");
    }

    public bool EndscriptExists(Func<string, bool> existsInModFolder) =>
        existsInModFolder(LauncherPaths.Normalize(Endscript));

    /// <summary>Files listed by the launcher that are missing in the game folder.</summary>
    public IReadOnlyList<string> MissingFiles(Func<string, bool> existsInGameFolder) =>
        Files.Where(file => !existsInGameFolder(LauncherPaths.Normalize(file))).ToList();
}

public static class LauncherPaths
{
    /// <summary>Launchers are written on Windows; convert backslashes for Android.</summary>
    public static string Normalize(string? path) => (path ?? string.Empty).Replace('\\', '/');
}

public static class GameMapping
{
    /// <summary>Maps a launcher game to the MainKeys game, or null when there is none.</summary>
    public static NfsGame? ToNfsGame(this LaunchGame game) => game switch
    {
        LaunchGame.Carbon => NfsGame.Carbon,
        LaunchGame.MostWanted => NfsGame.MostWanted,
        LaunchGame.Underground2 => NfsGame.Underground2,
        LaunchGame.Underground1 => NfsGame.Underground1,
        LaunchGame.Prostreet => NfsGame.ProStreet,
        LaunchGame.Undercover => NfsGame.Undercover,
        _ => null
    };
}

public static class EndLauncherReader
{
    private const string VersionHeader = "[VERSN1]";

    private static readonly JsonSerializerOptions Options = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Same rules as Launch.Deserialize: .endlauncher needs no version header,
    /// every other extension must start with [VERSN1]; backslashes are doubled before parsing.
    /// </summary>
    public static EndLauncher Parse(string text, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension != ".endlauncher")
        {
            if (!text.StartsWith(VersionHeader, StringComparison.Ordinal))
                throw new LauncherException("Endscript version was of invalid format, expected [VERSN1]");

            text = text[VersionHeader.Length..];
        }

        text = text.Replace(@"\", @"\\");

        EndLauncher? launcher;
        try
        {
            launcher = JsonSerializer.Deserialize<EndLauncher>(text, Options);
        }
        catch (JsonException ex)
        {
            throw new LauncherException($"Launcher is not valid JSON: {ex.Message}", ex);
        }

        if (launcher is null)
            throw new LauncherException("Launcher file is empty.");

        launcher.Files ??= new List<string>();
        launcher.Links ??= new List<LaunchLink>();
        return launcher;
    }
}