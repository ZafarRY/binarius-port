using System.Text;
using Binarius.Core.Launchers;
using Binarius.Core.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace Binarius.Android;

public sealed class UserModePage : ContentPage, IUserModePrompter
{
    private const int MaxListed = 40;

    private static readonly Color Good = Color.FromArgb("#65D6AD");
    private static readonly Color Bad = Color.FromArgb("#FF6B6B");

    private EndLauncher? _launcher;

    private AndroidSafFolder? _modFolder;
    private AndroidSafFolder? _gameFolder;

    private readonly Label _summary = new()
    {
        Text = "Select the mod folder to detect its End Launcher.",
        FontFamily = "monospace",
        FontSize = 13,
        TextColor = ToolUi.Primary
    };

    private readonly Label _details = new()
    {
        FontFamily = "monospace",
        FontSize = 12,
        TextColor = ToolUi.Muted
    };

    private readonly Label _modFolderLabel = new()
    {
        Text = "No mod folder selected.",
        TextColor = ToolUi.Muted,
        FontSize = 13
    };

    private readonly Label _gameFolderLabel = new()
    {
        Text = "No game folder selected.",
        TextColor = ToolUi.Muted,
        FontSize = 13
    };

    private readonly Label _status = new()
    {
        Text = "Select the mod folder to begin.",
        FontSize = 14,
        TextColor = ToolUi.Muted
    };

    private readonly Button _validateButton = new()
    {
        Text = "Validate Mod Package",
        BackgroundColor = Good,
        TextColor = ToolUi.Background,
        CornerRadius = 12,
        IsEnabled = false
    };

    private readonly Button _runButton = new()
    {
        Text = "Run Endscript",
        BackgroundColor = Color.FromArgb("#303A47"),
        TextColor = ToolUi.Muted,
        CornerRadius = 12,
        IsEnabled = false
    };

    public UserModePage()
    {
        Title = "User Mode";
        BackgroundColor = ToolUi.Background;

        var pickLauncher = new Button
        {
            Text = "Select End Launcher",
            BackgroundColor = Good,
            TextColor = ToolUi.Background,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 52
        };

        pickLauncher.Clicked += OnPickLauncher;

        var pickMod = new Button
        {
            Text = "Select Mod Folder",
            BackgroundColor = ToolUi.Surface,
            TextColor = ToolUi.Primary,
            CornerRadius = 12,
            HeightRequest = 48
        };

        pickMod.Clicked += OnPickModFolder;

        var pickGame = new Button
        {
            Text = "Select Game Folder",
            BackgroundColor = ToolUi.Surface,
            TextColor = ToolUi.Primary,
            CornerRadius = 12,
            HeightRequest = 48
        };

        pickGame.Clicked += OnPickGameFolder;

        _validateButton.Clicked += OnValidatePackage;

        _runButton.Clicked += OnRunEndscript;

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 24),
            Spacing = 16
        };

        content.Children.Add(new Label
        {
            Text = "Install a mod",
            TextColor = ToolUi.Primary,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold
        });

        content.Children.Add(pickLauncher);

        content.Children.Add(CreateCard(
            "END LAUNCHER",
            _summary));

        content.Children.Add(new Label
        {
            Text = "MOD PACKAGE",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Good
        });

        content.Children.Add(pickMod);
        content.Children.Add(CreateCard(
            "SELECTED MOD FOLDER",
            _modFolderLabel));

        content.Children.Add(new Label
        {
            Text = "GAME INSTALLATION",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Good
        });

        content.Children.Add(pickGame);
        content.Children.Add(CreateCard(
            "SELECTED GAME FOLDER",
            _gameFolderLabel));

        content.Children.Add(_validateButton);
        content.Children.Add(_runButton);
        content.Children.Add(_status);
        content.Children.Add(CreateCard("PACKAGE DETAILS", _details));

        Content = new ScrollView { Content = content };
    }

    private async void OnPickLauncher(object? sender, EventArgs e)
    {
        try
        {
            var picked = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Select End Launcher"
                });

            if (picked is null)
                return;

            string text;

            await using (var stream = await picked.OpenReadAsync())
            using (var reader = new StreamReader(stream))
            {
                text = await reader.ReadToEndAsync();
            }

            var launcher = EndLauncherReader.Parse(
                text, picked.FileName);

            launcher.EnsureUserMode();

            _launcher = launcher;

            ShowLauncher(picked.FileName, launcher);

            _modFolder = null;
            _gameFolder = null;

            _modFolderLabel.Text = "No mod folder selected.";
            _gameFolderLabel.Text = "No game folder selected.";

            _validateButton.IsEnabled = false;
            SetRunEnabled(false);

            SetStatus(
                $"Valid User Launcher: {launcher.GameId}. " +
                "Now select the mod folder and game folder.",
                Good);
        }
        catch (LauncherException ex)
        {
            _launcher = null;
            _validateButton.IsEnabled = false;
            SetStatus(ex.Message, Bad);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Could not open launcher",
                ex.Message,
                "OK");
        }
    }

    private async void OnPickModFolder(object? sender, EventArgs e)
    {
        try
        {
            var uri = await AndroidFolderPicker.PickAsync(
                "Select the mod package folder");

            if (uri is null)
                return;

            var folder = new AndroidSafFolder(uri);

            SetStatus("Looking for the End Launcher...", ToolUi.Muted);

            var (valid, problems) = await Task.Run(() => FindLaunchers(folder));

            if (valid.Count == 0)
            {
                _modFolder = null;
                _launcher = null;
                _validateButton.IsEnabled = false;
                SetRunEnabled(false);

                _modFolderLabel.Text = "No mod folder selected.";
                _summary.Text = "No valid User End Launcher found.";

                SetStatus(
                    problems.Count > 0
                        ? "No valid User End Launcher in this folder: " + problems[0]
                        : "No .endlauncher file found in this folder.",
                    Bad);
                return;
            }

            (string Path, EndLauncher Launcher) chosen = valid[0];

            if (valid.Count > 1)
            {
                var names = valid.Select(v => v.Path).ToArray();
                var pick = await DisplayActionSheetAsync(
                    "Multiple launchers found", "Cancel", null, names);

                if (pick is null || pick == "Cancel")
                    return;

                chosen = valid.First(v => v.Path == pick);
            }

            var launcher = chosen.Launcher;
            var gameChanged = _launcher is not null
                && !Equals(_launcher.GameId, launcher.GameId);

            _launcher = launcher;
            _modFolder = folder;

            ShowLauncher(System.IO.Path.GetFileName(chosen.Path), launcher);

            _modFolderLabel.Text = await folder.GetDisplayNameAsync();

            if (gameChanged)
            {
                _gameFolder = null;
                _gameFolderLabel.Text = "No game folder selected.";
            }

            _validateButton.IsEnabled = _gameFolder is not null;
            SetRunEnabled(false);

            SetStatus(
                _gameFolder is null
                    ? $"Found launcher for {launcher.GameId}. Select the game folder next."
                    : $"Found launcher for {launcher.GameId}. You can validate the package now.",
                Good);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not access mod folder: {ex.Message}", Bad);
        }
    }

    /// <summary>Finds valid User End Launchers: root of the folder first, then subfolders.</summary>
    private static (List<(string Path, EndLauncher Launcher)> Valid, List<string> Problems)
        FindLaunchers(AndroidSafFolder folder)
    {
        static bool IsLauncher(string path) =>
            path.EndsWith(".endlauncher", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".end", StringComparison.OrdinalIgnoreCase);
      
        var candidates = folder.ListRootFiles().Where(IsLauncher).ToList();

        if (candidates.Count == 0)
            candidates = folder.ListFiles().Where(IsLauncher).ToList();

        var valid = new List<(string, EndLauncher)>();
        var problems = new List<string>();

        foreach (var path in candidates)
        {
            try
            {
                string text;
                using (var stream = folder.OpenRead(path))
                using (var reader = new StreamReader(stream))
                    text = reader.ReadToEnd();

                var launcher = EndLauncherReader.Parse(
                    text, System.IO.Path.GetFileName(path));

                launcher.EnsureUserMode();
                valid.Add((path, launcher));
            }
            catch (LauncherException ex)
            {
                problems.Add($"{path}: {ex.Message}");
            }
            catch (Exception ex)
            {
                problems.Add($"{path}: {ex.Message}");
            }
        }

        return (valid, problems);
    }

    private async void OnPickGameFolder(object? sender, EventArgs e)
    {
        if (_launcher is null)
        {
            await DisplayAlertAsync(
                "Mod folder required",
                "Select the mod folder first so its End Launcher can be detected.",
                "OK");
            return;
        }

        try
        {
            var uri = await AndroidFolderPicker.PickAsync(
                $"Select Need for Speed {_launcher.GameId} folder");

            if (uri is null)
                return;

            _gameFolder = new AndroidSafFolder(uri);

            var name = await _gameFolder.GetDisplayNameAsync();

            _gameFolderLabel.Text = name;

            _validateButton.IsEnabled =
                _modFolder is not null;

            SetRunEnabled(false);

            SetStatus(
                "Game folder selected. Validate the mod package.",
                Good);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not access game folder: {ex.Message}", Bad);
        }
    }

    private async void OnValidatePackage(object? sender, EventArgs e)
    {
        if (_launcher is null ||
            _modFolder is null ||
            _gameFolder is null)
        {
            SetStatus("Select launcher, mod folder and game folder first.", Bad);
            return;
        }

        _validateButton.IsEnabled = false;

        try
        {
            var errors = new List<string>();

            // Check Endscript in the selected mod folder.
            if (!await _modFolder.ExistsAsync(_launcher.Endscript))
            {
                errors.Add($"Missing Endscript: {_launcher.Endscript}");
            }

            // Check launcher Files in the selected game folder.
            foreach (var file in _launcher.Files)
            {
                if (!await _gameFolder.ExistsAsync(file))
                    errors.Add($"Missing game file: {file}");
            }

            // Check Links according to original Endscript PathType.
            foreach (var link in _launcher.Links)
            {
                AndroidSafFolder? target = link.PType switch
                {
                    LinkPathType.Relative => _modFolder,
                    LinkPathType.Absolute => _gameFolder,
                    _ => null
                };

                if (target is null)
                {
                    errors.Add(
                        $"Invalid link path type: {link.PathType}");
                    continue;
                }

                if (!await target.ExistsAsync(link.File))
                {
                    errors.Add(
                        $"Missing link ({link.LoadType}): {link.File}");
                }
            }

            if (errors.Count > 0)
            {
                var sb = new StringBuilder();

                sb.AppendLine(
                    $"Package validation failed: {errors.Count} issue(s).");

                foreach (var error in errors.Take(MaxListed))
                    sb.AppendLine("• " + error);

                _details.Text = sb.ToString();

                SetStatus(
                    "Some launcher files are missing. Check package details.",
                    Bad);

                return;
            }

            _details.Text =
                $"Endscript : OK\n" +
                $"Game files: {_launcher.Files.Count} OK\n" +
                $"Links     : {_launcher.Links.Count} OK\n\n" +
                "All declared launcher paths were found.";

            SetStatus(
                "Launcher package is valid. You can run the endscript now.",
                Good);

            SetRunEnabled(true);
        }
        catch (Exception ex)
        {
            SetStatus($"Validation error: {ex.Message}", Bad);
        }
        finally
        {
            _validateButton.IsEnabled = true;
        }
    }

    private void ShowLauncher(
        string fileName,
        EndLauncher launcher)
    {
        var versioned = !string.Equals(
            System.IO.Path.GetExtension(fileName),
            ".endlauncher",
            StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder();

        sb.AppendLine($"File      : {fileName}");
        sb.AppendLine(
            $"Format    : {(versioned ? "VERSN1 launcher" : "endlauncher")}");
        sb.AppendLine($"Usage     : {launcher.Usage} ({launcher.UsageId})");
        sb.AppendLine($"Game      : {launcher.Game} ({launcher.GameId})");
        sb.AppendLine($"Endscript : {launcher.Endscript}");
        sb.AppendLine($"Files     : {launcher.Files.Count}");
        sb.Append($"Links     : {launcher.Links.Count}");

        _summary.Text = sb.ToString();

        var details = new StringBuilder();

        AppendList(details, "Files", launcher.Files);

        AppendList(
            details,
            "Links",
            launcher.Links.Select(
                l => $"{l.LType}/{l.PType}: {l.File}"));

        _details.Text = details.ToString().TrimEnd();
    }

    private static void AppendList(
        StringBuilder sb,
        string title,
        IEnumerable<string> items)
    {
        var list = items.ToList();

        if (list.Count == 0)
            return;

        sb.AppendLine($"{title}:");

        foreach (var item in list.Take(MaxListed))
            sb.AppendLine($"  {item}");

        if (list.Count > MaxListed)
            sb.AppendLine($"  ... +{list.Count - MaxListed} more");

        sb.AppendLine();
    }

    private static Border CreateCard(
        string title,
        Label content)
    {
        var layout = new VerticalStackLayout { Spacing = 10 };

        layout.Children.Add(new Label
        {
            Text = title,
            TextColor = ToolUi.Primary,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13
        });

        layout.Children.Add(content);

        return new Border
        {
            Stroke = ToolUi.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = ToolUi.Surface,
            Padding = 16,
            Content = layout
        };
    }

    private void SetStatus(string message, Color color)
    {
        _status.Text = message;
        _status.TextColor = color;
    }

    // ------------------------------------------------------------------ run

    private async void OnRunEndscript(object? sender, EventArgs e)
    {
        if (_launcher is null || _modFolder is null || _gameFolder is null) return;

        var launcher = _launcher;
        var mod = _modFolder;
        var game = _gameFolder;

        var proceed = await DisplayAlertAsync(
            "Install mod",
            "Binarius will work on a private copy first. Your game folder is only changed " +
            "after you choose to save, and original files are kept as .bacc backups.",
            "Continue",
            "Cancel");
        if (!proceed) return;

        if (!UserModeRunner.HasAllFilesAccess())
        {
            var open = await DisplayAlertAsync(
                "Log folder",
                "To save MainLog.txt in Internal storage/Binarius/logs, allow \"All files access\" " +
                "for Binarius. Otherwise the log is kept in the app's private folder.",
                "Open settings",
                "Skip");
            if (open)
            {
                UserModeRunner.OpenAllFilesAccessSettings();
                return; // run again after granting the permission
            }
        }

        SetRunEnabled(false);
        _validateButton.IsEnabled = false;

        var workDir = System.IO.Path.Combine(FileSystem.CacheDirectory, "usermode");
        var keysDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "keys");

        void Log(string message) =>
            MainThread.BeginInvokeOnMainThread(() => SetStatus(message, ToolUi.Muted));

        try
        {
            SetStatus("Preparing files...", ToolUi.Muted);
            var staged = await Task.Run(() => UserModeStager.Stage(launcher, mod, game, workDir, Log));

            await EnsureKeysAsync(keysDir, launcher);

            var outcome = await UserModeRunner.RunAsync(launcher, staged, keysDir, this, Log);
            var logPath = await Task.Run(() => WriteRunLog(launcher, outcome));

            if (outcome.Failure is not null)
            {
                SetStatus("The endscript could not be applied. No game files were changed.", Bad);
                _details.Text = outcome.Failure + LogNote(logPath);
                return;
            }

            if (!outcome.Saved)
            {
                _details.Text = FormatErrors(outcome) + LogNote(logPath);
                SetStatus(
                    outcome.SaveErrors.Count > 0
                        ? "Saving failed. No game files were changed."
                        : "Not saved. No game files were changed.",
                    outcome.SaveErrors.Count > 0 ? Bad : ToolUi.Warning);
                return;
            }

            SetStatus("Writing changes to the game folder...", ToolUi.Muted);
            var result = await Task.Run(() => UserModeStager.WriteBack(staged, game, Log));

            _details.Text =
                $"Written  : {result.Written}\n" +
                $"Deleted  : {result.Deleted}\n" +
                $"Backups  : {result.Backups} (.bacc)" +
                (outcome.ScriptErrors.Count > 0 ? "\n\n" + FormatErrors(outcome) : string.Empty) +
                LogNote(logPath);

            SetStatus(
                outcome.ScriptErrors.Count == 0
                    ? "Mod installed successfully."
                    : $"Mod installed with {outcome.ScriptErrors.Count} script error(s).",
                outcome.ScriptErrors.Count == 0 ? Good : ToolUi.Warning);
        }
        catch (LauncherException ex)
        {
            SetStatus(ex.Message, Bad);
        }
        catch (Exception ex)
        {
            SetStatus($"Unexpected error: {ex.Message}", Bad);
        }
        finally
        {
            try { if (System.IO.Directory.Exists(workDir)) System.IO.Directory.Delete(workDir, true); }
            catch { /* cache is cleared on the next run anyway */ }

            _validateButton.IsEnabled = true;
            SetRunEnabled(true);
        }
    }

    private static string LogNote(string? path) =>
        path is null ? string.Empty : $"\n\nFull log: {path}";

    /// <summary>Writes every failure and script error of the last run to RunLog.txt.</summary>
    private static string? WriteRunLog(EndLauncher launcher, UserModeOutcome outcome)
    {
        try
        {
            var path = System.IO.Path.Combine(UserModeRunner.ResolveLogDir(), "RunLog.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"Binarius run : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Game         : {launcher.GameId}");
            sb.AppendLine($"Endscript    : {launcher.Endscript}");

            if (outcome.Failure is not null)
            {
                sb.AppendLine();
                sb.AppendLine("FAILURE:");
                sb.AppendLine(outcome.Failure);
            }

            if (outcome.ScriptErrors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"SCRIPT ERRORS ({outcome.ScriptErrors.Count}):");
                foreach (var error in outcome.ScriptErrors)
                    sb.AppendLine(error);
            }

            if (outcome.SaveErrors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"SAVE ERRORS ({outcome.SaveErrors.Count}):");
                foreach (var error in outcome.SaveErrors)
                    sb.AppendLine(error);
            }

            System.IO.File.WriteAllText(path, sb.ToString());
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatErrors(UserModeOutcome outcome)
    {
        var sb = new StringBuilder();
        AppendList(sb, "Script errors", outcome.ScriptErrors);
        AppendList(sb, "Save errors", outcome.SaveErrors);
        return sb.ToString().TrimEnd();
    }

    /// <summary>The hash lists live in the app package; the engine needs real files.</summary>
    private static async Task EnsureKeysAsync(string keysDir, EndLauncher launcher)
    {
        if (launcher.GameId.ToNfsGame() is not { } game) return;

        var name = KeyBook.FileName(game);
        var target = System.IO.Path.Combine(keysDir, "mainkeys", name);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);

        await using var source = await KeyAssets.OpenAsync(name);
        await using var output = System.IO.File.Create(target);
        await source.CopyToAsync(output);
    }

    private void SetRunEnabled(bool enabled)
    {
        _runButton.IsEnabled = enabled;
        _runButton.BackgroundColor = enabled ? Good : Color.FromArgb("#303A47");
        _runButton.TextColor = enabled ? ToolUi.Background : ToolUi.Muted;
    }

    // ------------------------------------------------------------------ IUserModePrompter

    public Task ShowInfoAsync(string message) =>
        DisplayAlertAsync("Information", message, "OK");

    public Task<bool> AskCheckboxAsync(string message) =>
        DisplayAlertAsync("Option", message, "Enabled", "Disabled");

    public async Task<int> AskChoiceAsync(string message, IReadOnlyList<string> options)
    {
        var choice = await DisplayActionSheetAsync(message, null, null, options.ToArray());
        return choice is null ? -1 : options.ToList().IndexOf(choice);
    }

    public Task<bool> ConfirmSaveAsync(string summary) =>
        DisplayAlertAsync("Done", summary + "\n\nWould you like to save the files?", "Save", "Discard");
}