using System.Text.RegularExpressions;

namespace Binarius.Core.Launchers;

public static class ScriptPathFixer
{
    private static readonly Regex Token = new("\"[^\"]*\"|\\S+", RegexOptions.Compiled);
    private static readonly Regex PathLike = new(@"^(?:[^\\/:*?""<>|]+\\)+[^\\/:*?""<>|]*$", RegexOptions.Compiled);
    private static readonly HashSet<string> TextCommands = new(StringComparer.Ordinal)
    { "infobox", "checkbox", "combobox" };

    public static string Fix(string text, Func<string, string?> resolve)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++) lines[i] = FixLine(lines[i], resolve);
        return string.Join('\n', lines);
    }

    public static bool IsScript(string fileName, string text)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".end" && ext != ".endscript") return false;
        var head = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (head.StartsWith("[VERSN1]", StringComparison.Ordinal)) return false;
        if (head.StartsWith("[VERSN3]", StringComparison.Ordinal)) return false;
        return true;
    }

    private static string FixLine(string line, Func<string, string?> resolve)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal) ||
            trimmed[0] is '#' or '{' or '}')
            return line;

        var first = true;
        string command = string.Empty;

        return Token.Replace(line, m =>
        {
            if (first) { first = false; command = m.Value; return m.Value; }
            return TextCommands.Contains(command) ? m.Value : FixToken(m.Value, resolve);
        });
    }

    private static string FixToken(string value, Func<string, string?> resolve)
    {
        var quoted = value.Length >= 2 && value[0] == '"' && value[^1] == '"';
        var inner = quoted ? value[1..^1] : value;
        if (!inner.Contains('\\') || !PathLike.IsMatch(inner)) return value;

        var path = inner.Replace('\\', '/');
        var trailing = path.EndsWith('/');
        var real = resolve(path.TrimEnd('/'));
        if (real is not null) path = real + (trailing ? "/" : string.Empty);

        return quoted ? "\"" + path + "\"" : path;
    }
}