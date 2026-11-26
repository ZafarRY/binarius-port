using System.Text;
using Binarius.Core.Launchers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace Binarius.Android;

public sealed class UserModePage : ContentPage
{
    private const int MaxListed = 40;

    private static readonly Color Good = Color.FromArgb("#65D6AD");
    private static readonly Color Bad = Color.FromArgb("#FF6B6B");

    private readonly Label _summary = new()
    {
        Text = "No End Launcher selected yet.",
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

    private readonly Label _status = new() { FontSize = 14, TextColor = ToolUi.Muted };

    public UserModePage()
    {
        Title = "User Mode";
        BackgroundColor = ToolUi.Background;

        var pick = new Button
        {
            Text = "Select End Launcher",
            BackgroundColor = Good,
            TextColor = ToolUi.Background,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 48
        };
        pick.Clicked += OnPickClicked;

        var card = new Border
        {
            Stroke = ToolUi.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = ToolUi.Surface,
            Padding = 16,
            Content = _summary
        };

        var content = new VerticalStackLayout { Padding = new Thickness(20, 24), Spacing = 16 };
        content.Children.Add(new Label
        {
            Text = "Install a mod",
            TextColor = ToolUi.Primary,
            FontSize = 26,
            FontAttributes = FontAttributes.Bold
        });
        content.Children.Add(pick);
        content.Children.Add(card);
        content.Children.Add(_status);
        content.Children.Add(_details);

        Content = new ScrollView { Content = content };
    }

    private async void OnPickClicked(object? sender, EventArgs e)
    {
        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select End Launcher"
            });
            if (picked is null) return;

            string text;
            await using (var stream = await picked.OpenReadAsync())
            using (var reader = new StreamReader(stream))
            {
                text = await reader.ReadToEndAsync();
            }

            var launcher = EndLauncherReader.Parse(text, picked.FileName);
            Show(picked.FileName, launcher);

            launcher.EnsureUserMode();
            SetStatus(
                $"Valid launcher for User Mode ({launcher.GameId}). " +
                "Next steps: choose the mod folder and the game folder, then run the endscript " +
                "(not active yet).",
                Good);
        }
        catch (LauncherException ex)
        {
            SetStatus(ex.Message, Bad);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Could not open the file", ex.Message, "OK");
        }
    }

    private void Show(string fileName, EndLauncher launcher)
    {
        var versioned = !string.Equals(
            System.IO.Path.GetExtension(fileName), ".endlauncher", StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.AppendLine($"File      : {fileName}");
        sb.AppendLine($"Format    : {(versioned ? "VERSN1 launcher" : "endlauncher")}");
        sb.AppendLine($"Usage     : {launcher.Usage} ({launcher.UsageId})");
        sb.AppendLine($"Game      : {launcher.Game} ({launcher.GameId})");
        sb.AppendLine($"Endscript : {launcher.Endscript}");
        sb.AppendLine($"Files     : {launcher.Files.Count}");
        sb.Append($"Links     : {launcher.Links.Count}");
        _summary.Text = sb.ToString();

        var details = new StringBuilder();
        AppendList(details, "Files", launcher.Files);
        AppendList(details, "Links", launcher.Links.Select(l => $"{l.LType}/{l.PType}: {l.File}"));
        _details.Text = details.ToString().TrimEnd();
    }

    private static void AppendList(StringBuilder sb, string title, IEnumerable<string> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        sb.AppendLine($"{title}:");
        foreach (var item in list.Take(MaxListed)) sb.AppendLine($"  {item}");
        if (list.Count > MaxListed) sb.AppendLine($"  ... +{list.Count - MaxListed} more");
        sb.AppendLine();
    }

    private void SetStatus(string message, Color color)
    {
        _status.Text = message;
        _status.TextColor = color;
    }
}