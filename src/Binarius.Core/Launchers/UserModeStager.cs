using System.Security.Cryptography;
using System.Text;
using Binarius.Core.Storage;
using System.Text.RegularExpressions;

namespace Binarius.Core.Launchers;

public sealed class StagedLaunch
{
    public string ModDir { get; init; } = string.Empty;
    public string GameDir { get; init; } = string.Empty;
    public string Endscript { get; init; } = string.Empty;
    public List<string> Files { get; } = new();
    public List<LaunchLink> Links { get; } = new();
    public Dictionary<string, string> GameMap { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Snapshot { get; } = new(StringComparer.Ordinal);
}

public sealed record WriteBackResult(int Written, int Deleted, int Backups);

public static class UserModeStager
{
    public static StagedLaunch Stage(
        EndLauncher launcher, IFileTree mod, IFileTree game, string workDir, Action<string>? log = null)
    {
        if (Directory.Exists(workDir)) Directory.Delete(workDir, true);
        var modDir = Path.Combine(workDir, "mod");
        var gameDir = Path.Combine(workDir, "game");
        Directory.CreateDirectory(modDir);
        Directory.CreateDirectory(gameDir);

        var modFiles = mod.ListFiles();
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < modFiles.Count; i++)
        {
            var rel = modFiles[i];
            CopyOut(mod, rel, Path.Combine(modDir, rel));
            AddToIndex(index, rel);
            if (i % 20 == 0) log?.Invoke($"Copying mod files {i + 1}/{modFiles.Count}");
        }

        string? Real(string path) => index.TryGetValue(path, out var real) ? real : null;

        foreach (var rel in modFiles)
        {
            var local = Path.Combine(modDir, rel);
            var ext = Path.GetExtension(rel).ToLowerInvariant();
            if (ext != ".end" && ext != ".endscript") continue;

            var text = File.ReadAllText(local);
            if (!ScriptPathFixer.IsScript(rel, text)) continue;
            File.WriteAllText(local, ScriptPathFixer.Fix(text, Real), new UTF8Encoding(false));
        }

        var endscriptRel = LauncherPaths.Normalize(launcher.Endscript).Trim('/');
        var endscript = Real(endscriptRel)
            ?? throw new LauncherException($"Endscript not found in the mod folder: {launcher.Endscript}");

        var staged = new StagedLaunch { ModDir = modDir, GameDir = gameDir, Endscript = endscript };

        foreach (var file in launcher.Files)
        {
            var wanted = LauncherPaths.Normalize(file).Trim('/');
            var source = game.Resolve(wanted)
                ?? throw new LauncherException($"Game file not found: {file}");

            var localRel = wanted.ToUpperInvariant();
            log?.Invoke($"Copying game file {wanted}");
            CopyOut(game, source, Path.Combine(gameDir, localRel));
            staged.GameMap[localRel] = source;
            staged.Files.Add(localRel);
        }

        foreach (var link in launcher.Links)
        {
            var wanted = LauncherPaths.Normalize(link.File).Trim('/');
            var copy = new LaunchLink { LoadType = link.LoadType, PathType = link.PathType };

            if (link.PType == LinkPathType.Relative)
            {
                copy.File = Real(wanted)
                    ?? throw new LauncherException($"Link file not found in the mod folder: {link.File}");
            }
            else
            {
                var source = game.Resolve(wanted)
                    ?? throw new LauncherException($"Link file not found in the game folder: {link.File}");
                log?.Invoke($"Copying link file {wanted}");
                CopyOut(game, source, Path.Combine(gameDir, wanted));
                staged.GameMap[wanted] = source;
                copy.File = wanted;
            }
            staged.Links.Add(copy);
        }

        StageReferencedGameFiles(game, modDir, modFiles, gameDir, staged, log);

        foreach (var path in Directory.EnumerateFiles(gameDir, "*", SearchOption.AllDirectories))
            staged.Snapshot[RelativeTo(gameDir, path)] = Hash(path);

        return staged;
    }

    public static WriteBackResult WriteBack(StagedLaunch staged, IFileTree game, Action<string>? log = null)
    {
        int written = 0, deleted = 0, backups = 0;

        var current = Directory.EnumerateFiles(staged.GameDir, "*", SearchOption.AllDirectories)
            .Select(p => RelativeTo(staged.GameDir, p)).ToList();

        foreach (var rel in current)
        {
            var local = Path.Combine(staged.GameDir, rel);
            if (staged.Snapshot.TryGetValue(rel, out var before) && before == Hash(local)) continue;

            var target = staged.GameMap.TryGetValue(rel, out var mapped) ? mapped : game.Resolve(rel) ?? rel;
            if (Backup(game, target)) backups++;

            log?.Invoke($"Writing {target}");
            using var input = File.OpenRead(local);
            game.Write(target, input);
            written++;
        }

        var present = new HashSet<string>(current, StringComparer.Ordinal);
        foreach (var rel in staged.Snapshot.Keys.Where(k => !present.Contains(k)).ToList())
        {
            if (!staged.GameMap.TryGetValue(rel, out var target)) continue;
            if (Backup(game, target)) backups++;
            log?.Invoke($"Deleting {target}");
            game.Delete(target);
            deleted++;
        }

        return new WriteBackResult(written, deleted, backups);
    }

    private static readonly Regex ScriptTokens = new("\"[^\"]*\"|\\S+", RegexOptions.Compiled);

    /// <summary>
    /// move_file / erase_file may touch game files that the launcher does not list.
    /// Copy those (when they exist in the game folder) into the working copy.
    /// </summary>
    private static void StageReferencedGameFiles(
        IFileTree game, string modDir, IReadOnlyList<string> modFiles,
        string gameDir, StagedLaunch staged, Action<string>? log)
    {
        foreach (var rel in modFiles)
        {
            var ext = Path.GetExtension(rel).ToLowerInvariant();
            if (ext != ".end" && ext != ".endscript") continue;

            string[] lines;
            try { lines = File.ReadAllLines(Path.Combine(modDir, rel)); }
            catch { continue; }

            foreach (var line in lines)
            {
                var t = ScriptTokens.Matches(line)
                    .Select(m => m.Value.Trim('"'))
                    .ToArray();

                string? wanted = null;
                if (t.Length == 6 && t[0] == "move_file" && t[2] == "absolute") wanted = t[4];
                else if (t.Length == 3 && t[0] == "erase_file" && t[1] == "absolute") wanted = t[2];
                if (wanted is null) continue;

                var path = LauncherPaths.Normalize(wanted).Trim('/');
                if (path.Length == 0 || staged.GameMap.ContainsKey(path)) continue;

                var local = Path.Combine(gameDir, path);
                if (File.Exists(local)) continue;

                var source = game.Resolve(path);
                if (source is null) continue;

                log?.Invoke($"Copying game file {path}");
                CopyOut(game, source, local);
                staged.GameMap[path] = source;
            }
        }
    }

    private static bool Backup(IFileTree game, string target)
    {
        if (target.EndsWith(".bacc", StringComparison.OrdinalIgnoreCase)) return false;
        if (game.Resolve(target) is null) return false;
        if (game.Resolve(target + ".bacc") is not null) return false;

        using var original = game.OpenRead(target);
        game.Write(target + ".bacc", original);
        return true;
    }

    private static void CopyOut(IFileTree tree, string rel, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var input = tree.OpenRead(rel);
        using var output = File.Create(destination);
        input.CopyTo(output);
    }

    private static void AddToIndex(Dictionary<string, string> index, string rel)
    {
        index[rel] = rel;
        var slash = rel.LastIndexOf('/');
        while (slash > 0)
        {
            var folder = rel[..slash];
            index.TryAdd(folder, folder);
            slash = folder.LastIndexOf('/');
        }
    }

    private static string RelativeTo(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}