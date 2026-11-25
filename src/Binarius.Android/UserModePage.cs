using Binarius.Core.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Binarius.Android;

public sealed class UserModePage : ContentPage
{
    private static readonly string[] LauncherExtensions = [".end", ".endlauncher"];

    private readonly FileInspector _inspector = new();
    private readonly Label _summary;
    private readonly Label _status;

    public UserModePage()
    {
        Title = "User Mode";
        BackgroundColor = Color.FromArgb("#101318");

        _summary = new Label
        {
            Text = "Belum ada End Launcher dipilih.",
            TextColor = Color.FromArgb("#F3F5F7"),
            FontSize = 14
        };
        _status = new Label
        {
            Text = "Pilih file .end / .endlauncher untuk memasang mod.",
            TextColor = Color.FromArgb("#65D6AD"),
            FontSize = 13
        };

        var pick = new Button
        {
            Text = "Pilih End Launcher",
            BackgroundColor = Color.FromArgb("#65D6AD"),
            TextColor = Color.FromArgb("#101318"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 48
        };
        pick.Clicked += OnPickClicked;

        var content = new VerticalStackLayout { Padding = new Thickness(20, 28), Spacing = 16 };
        content.Children.Add(new Label
        {
            Text = "User Mode",
            TextColor = Color.FromArgb("#F3F5F7"),
            FontSize = 30,
            FontAttributes = FontAttributes.Bold
        });
        content.Children.Add(new Label
        {
            Text = "Install a mod",
            TextColor = Color.FromArgb("#AAB4C0"),
            FontSize = 13
        });
        content.Children.Add(pick);
        content.Children.Add(_summary);
        content.Children.Add(_status);

        Content = new ScrollView { Content = content };
    }

    private async void OnPickClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pilih End Launcher"
            });
            if (file is null) return;

            // Android tidak bisa memfilter per ekstensi, jadi validasi manual.
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!LauncherExtensions.Contains(ext))
            {
                await DisplayAlertAsync("File tidak sesuai", "Pilih file .end atau .endlauncher.", "OK");
                return;
            }

            var dir = Path.Combine(FileSystem.AppDataDirectory, "workspace", "user");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}");

            await using (var src = await file.OpenReadAsync())
            await using (var dst = File.Create(path))
            {
                await src.CopyToAsync(dst);
            }

            await using var stream = File.OpenRead(path);
            var info = await _inspector.InspectAsync(stream, file.FileName);

            _summary.Text = $"{info.Name}\n{info.Length:N0} byte\nWorkspace copy: {Path.GetFileName(path)}";
            _status.Text = "File tersalin ke workspace privat. Cek Usage/Game dan eksekusi script " +
                           "menunggu port modul Endscript.";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Gagal membuka file", ex.Message, "OK");
        }
    }
}