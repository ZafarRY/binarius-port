
using Endscript.Core;
using Endscript.Commands;

namespace Binarius.Android;

public sealed record EndscriptCommandInfo(
    int Number,
    string Type,
    string File,
    int Line,
    string Command
);

public sealed record EndscriptParseResult(
    string ScriptPath,
    int TotalCommands,
    IReadOnlyList<EndscriptCommandInfo> Commands
);

public static class EndscriptPreviewService
{
    public static EndscriptParseResult Parse(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                "Endscript file not found.",
                scriptPath);
        }

        var parser = new EndScriptParser(scriptPath);

        try
        {
            BaseCommand[] commands = parser.Read();

            var results = commands
                .Select((command, index) =>
                    new EndscriptCommandInfo(
                        index + 1,
                        command.Type.ToString(),
                        command.Filename,
                        command.Index,
                        command.Line))
                .ToList();

            return new EndscriptParseResult(
                scriptPath,
                results.Count,
                results);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Endscript parsing failed.\n" +
                $"File: {parser.CurrentFile}\n" +
                $"Line: {parser.CurrentIndex}\n" +
                $"Command: {parser.CurrentLine}\n" +
                $"Reason: {ex.GetBaseException().Message}",
                ex);
        }
    }
}
