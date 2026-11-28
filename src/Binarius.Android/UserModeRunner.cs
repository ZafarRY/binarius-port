using Binarius.Core.Launchers;
using CoreExtensions.Management;
using Endscript.Commands;
using Endscript.Core;
using Endscript.Helpers;
using Endscript.Profiles;

namespace Binarius.Android;

internal static class UserModeRunner
{
        public static async Task<UserModeOutcome> RunAsync(
        EndLauncher launcher, StagedLaunch staged, string keysDir,
        IUserModePrompter prompter, Action<string> log)
    {
        var outcome = new UserModeOutcome();
        ConfigureEngine(keysDir);

        // The engine writes relative files (e.g. MainLog.txt). The default working
        // directory on Android is "/" (read-only), so point it at a writable log folder.
        System.Environment.CurrentDirectory = ResolveLogDir();

        var launch = new Launch
        {
            Usage = launcher.Usage,
            Game = launcher.Game,
            Directory = staged.GameDir,
            ThisDir = staged.ModDir,
            Endscript = staged.Endscript,
            Files = staged.Files.ToList(),
            Links = staged.Links.Select(l => new SubLoader
            {
                LoadType = l.LoadType, PathType = l.PathType, File = l.File
            }).ToList()
        };

        // ... sisanya tidak berubah

        var endscriptPath = System.IO.Path.Combine(staged.ModDir, staged.Endscript);
        var parser = new EndScriptParser(endscriptPath);

        try
        {
            log("Checking launcher files...");
            await Task.Run(() => { launch.CheckEndscript(); launch.CheckFiles(); launch.LoadLinks(); });
        }
        catch (Exception ex)
        {
            outcome.Fail($"Cannot prepare the launcher: {ex.GetLowestMessage()}");
            return outcome;
        }

        BaseCommand[] commands;
        try
        {
            log("Reading endscript...");
            commands = await Task.Run(() => parser.Read());
        }
        catch (Exception ex)
        {
            outcome.Fail(
                $"Error in {parser.CurrentFile}, line {parser.CurrentIndex}\n" +
                $"Command: [{parser.CurrentLine}]\nError: {ex.GetLowestMessage()}");
            return outcome;
        }

        var profile = BaseProfile.NewProfile(launch.GameID, launch.Directory);

        log("Loading game files...");
        string[] loadErrors;
        try { loadErrors = await Task.Run(() => profile.Load(launch)); }
        catch (Exception ex)
        {
            outcome.Fail($"Cannot load the game files: {ex.GetLowestMessage()}");
            return outcome;
        }

        if (loadErrors.Length > 0)
        {
            outcome.Fail(string.Join("\n", loadErrors) + "\n\nUnable to execute the endscript because of these errors.");
            return outcome;
        }

        var manager = new EndScriptManager(profile, commands, endscriptPath);

        try
        {
            log("Running endscript...");
            manager.CommandChase();

            while (!await Task.Run(() => manager.ProcessScript()))
            {
                switch (manager.CurrentCommand)
                {
                    case InfoboxCommand info:
                        await prompter.ShowInfoAsync(info.Description);
                        break;
                    case CheckboxCommand checkbox:
                        checkbox.Choice = await prompter.AskCheckboxAsync(checkbox.Description) ? 1 : 0;
                        break;
                    case ComboboxCommand combobox:
                    {
                        var names = combobox.Options.Select(o => o.Name).ToList();
                        var picked = await prompter.AskChoiceAsync(combobox.Description, names);
                        combobox.Choice = picked < 0 ? 0 : picked;
                        break;
                    }
                    default:
                        outcome.Fail("The endscript is waiting for an input type that is not supported.");
                        return outcome;
                }
            }
        }
        catch (Exception ex)
        {
            outcome.Fail($"Execution was interrupted: {ex.GetLowestMessage()}");
            return outcome;
        }

        foreach (var error in manager.Errors)
            outcome.ScriptErrors.Add($"{error.Filename}:{error.Index} [{error.Line}] -> {error.Error}");

        outcome.Applied = true;

        var summary = outcome.ScriptErrors.Count == 0
            ? "The script has been applied successfully."
            : $"The script has been applied, but {outcome.ScriptErrors.Count} error(s) were detected.";

        if (!await prompter.ConfirmSaveAsync(summary)) return outcome;

        log("Saving files...");
        string[] saveErrors;
        try { saveErrors = await Task.Run(() => profile.Save()); }
        catch (Exception ex)
        {
            outcome.SaveErrors.Add(ex.GetLowestMessage());
            return outcome;
        }

        outcome.SaveErrors.AddRange(saveErrors);
        outcome.Saved = saveErrors.Length == 0;
        return outcome;
    }

    /// <summary>Public log folder if writable, otherwise the app's private folder.</summary>
    internal static string ResolveLogDir()
    {
        try
        {
            var root = global::Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath
                       ?? "/storage/emulated/0";
            var dir = System.IO.Path.Combine(root, "Binarius", "logs");
            System.IO.Directory.CreateDirectory(dir);

            var probe = System.IO.Path.Combine(dir, ".write_test");
            System.IO.File.WriteAllText(probe, string.Empty);
            System.IO.File.Delete(probe);
            return dir;
        }
        catch
        {
            var dir = System.IO.Path.Combine(
                Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "logs");
            System.IO.Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>True when the app may write to /storage/emulated/0 directly.</summary>
    internal static bool HasAllFilesAccess() =>
        !OperatingSystem.IsAndroidVersionAtLeast(30)
        || global::Android.OS.Environment.IsExternalStorageManager;

    /// <summary>Opens the "All files access" settings screen for this app.</summary>
    internal static void OpenAllFilesAccessSettings()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30)) return;
      
        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(
            global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission,
            global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
    }

    private static void ConfigureEngine(string keysDir)
    {
        string Main(string name) => System.IO.Path.Combine(keysDir, "mainkeys", name);
        string User(string name) => System.IO.Path.Combine(keysDir, "userkeys", name);

        CarbonProfile.MainHashList = Main("carbon.txt");
        CarbonProfile.CustomHashList = User("carbon.txt");
        MostWantedProfile.MainHashList = Main("mostwanted.txt");
        MostWantedProfile.CustomHashList = User("mostwanted.txt");
        ProstreetProfile.MainHashList = Main("prostreet.txt");
        ProstreetProfile.CustomHashList = User("prostreet.txt");
        UndercoverProfile.MainHashList = Main("undercover.txt");
        UndercoverProfile.CustomHashList = User("undercover.txt");
        Underground1Profile.MainHashList = Main("underground1.txt");
        Underground1Profile.CustomHashList = User("underground1.txt");
        Underground2Profile.MainHashList = Main("underground2.txt");
        Underground2Profile.CustomHashList = User("underground2.txt");
        global::Nikki.Core.Map.CustomAttribFile = User("CustomAttributes.txt");

        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(keysDir, "mainkeys"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(keysDir, "userkeys"));
        foreach (var name in new[]
        {
            "carbon.txt", "mostwanted.txt", "prostreet.txt", "undercover.txt",
            "underground1.txt", "underground2.txt", "CustomAttributes.txt"
        })
        {
            var path = User(name);
            if (!System.IO.File.Exists(path)) System.IO.File.WriteAllText(path, string.Empty);
        }

        global::Endscript.Version.Value = new System.Version(2, 10, 4, 0);
        SynchronizedDatabase.Watermark = "Binarius | Automated";
    }
}