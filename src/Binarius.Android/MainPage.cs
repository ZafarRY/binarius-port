using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Binarius.Core.Models;
using Binarius.Core.Services;

namespace Binarius.Android;

public sealed class MainPage : ContentPage
{
    private readonly FileInspector _inspector = new();
    private readonly Label _fileSummary;
    private readonly Label _headerPreview;
    private readonly Label _status;

    public MainPage()
    {
        Title = "Binarius Mobile";
        BackgroundColor = Color.FromArgb("#101318");

        _fileSummary = new Label
        {
            Text = "Belum ada file yang dibuka.",
            TextColor = Color.FromArgb("#F3F5F7"),
            FontSize = 14
        };
        _headerPreview = new Label
        {
            Text = "Header preview akan muncul setelah file dipilih.",
            TextColor = Color.FromArgb("#AAB4C0"),
            FontSize = 12,
            FontFamily = "monospace"
        };
        _status = new Label
        {
            Text = "Fondasi Android tersedia. Engine editor desktop belum terhubung.",
            TextColor = Color.FromArgb("#65D6AD"),
            FontSize = 13
        };

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 28),
            Spacing = 16
        };

        content.Children.Add(new Label
        {
            Text = "BINARIUS",
            TextColor = Color.FromArgb("#65D6AD"),
            FontSize = 13,
            CharacterSpacing = 3,
            FontAttributes = FontAttributes.Bold
        });
        content.Children.Add(new Label
        {
            Text = "Need for Speed\nBinary Editor",
            TextColor = Color.FromArgb("#F3F5F7"),
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            LineHeight = 1.05
        });
        content.Children.Add(new Label
        {
            Text = "Android ARM64 · .NET MAUI",
            TextColor = Color.FromArgb("#AAB4C0"),
            FontSize = 13
        });

        var openButton = new Button
        {
            Text = "Buka file",
            BackgroundColor = Color.FromArgb("#65D6AD"),
            TextColor = Color.FromArgb("#101318"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 48
        };
        openButton.Clicked += OnOpenFileClicked;
        content.Children.Add(CreateCard(
            "FILE WORKSPACE",
            "Pilih file .BIN, .BUN, .LZC, atau .END dari penyimpanan perangkat.",
            _fileSummary,
            _headerPreview,
            openButton));

        content.Children.Add(new Label
        {
            Text = "FEATURE PORTING STATUS",
            TextColor = Color.FromArgb("#65D6AD"),
            FontSize = 12,
            CharacterSpacing = 1.5,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var feature in FeatureCatalog.All)
            content.Children.Add(CreateFeatureCard(feature));

        content.Children.Add(new Border
        {
            Stroke = Color.FromArgb("#303A47"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Padding = 14,
            Content = _status
        });

        Content = new ScrollView { Content = content };
    }

    private static Border CreateCard(
        string title,
        string description,
        Label fileSummary,
        Label headerPreview,
        Button action)
    {
        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(new Label
        {
            Text = title,
            TextColor = Color.FromArgb("#F3F5F7"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        });
        layout.Children.Add(new Label
        {
            Text = description,
            TextColor = Color.FromArgb("#AAB4C0"),
            FontSize = 13
        });
        layout.Children.Add(fileSummary);
        layout.Children.Add(headerPreview);
        layout.Children.Add(action);

        return new Border
        {
            Stroke = Color.FromArgb("#303A47"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Color.FromArgb("#1A2029"),
            Padding = 16,
            Content = layout
        };
    }

    private Border CreateFeatureCard(FeatureDefinition feature)
    {
        var layout = new VerticalStackLayout { Spacing = 5 };
        layout.Children.Add(new Label
        {
            Text = feature.Name,
            TextColor = Color.FromArgb("#F3F5F7"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        });
        layout.Children.Add(new Label
        {
            Text = feature.Description,
            TextColor = Color.FromArgb("#AAB4C0"),
            FontSize = 12
        });
        layout.Children.Add(new Label
        {
            Text = feature.StateLabel,
            TextColor = feature.State is PortingState.Implemented or PortingState.SourcePortedPendingValidation
                ? Color.FromArgb("#65D6AD")
                : Color.FromArgb("#E7B96A"),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold
        });

        if (feature.Name == "Swatch Picker")
        {
            var openTool = new Button
            {
                Text = "Buka Swatch Picker",
                BackgroundColor = Color.FromArgb("#65D6AD"),
                TextColor = Color.FromArgb("#101318"),
                CornerRadius = 10
            };
            openTool.Clicked += async (_, _) => await Navigation.PushAsync(new SwatchPickerPage());
            layout.Children.Add(openTool);
        }

        return new Border
        {
            Stroke = Color.FromArgb("#303A47"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            BackgroundColor = Color.FromArgb("#1A2029"),
            Padding = 14,
            Content = layout
        };
    }

    private async void OnOpenFileClicked(object? sender, EventArgs e)
    {
        try
        {
            var selected = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pilih file data Need for Speed"
            });

            if (selected is null) return;

            var workspaceDirectory = Path.Combine(FileSystem.AppDataDirectory, "workspace");
            Directory.CreateDirectory(workspaceDirectory);
            var safeName = Path.GetFileName(selected.FileName);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "selected-file.bin";
            var workspacePath = Path.Combine(workspaceDirectory, $"{Guid.NewGuid():N}_{safeName}");

            // Android may expose a content:// URI. Copy the selected stream into
            // app-private storage instead of treating FullPath as a normal path.
            await using (var source = await selected.OpenReadAsync())
            await using (var destination = new FileStream(workspacePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await source.CopyToAsync(destination);
            }

            await using var stream = File.OpenRead(workspacePath);
            var info = await _inspector.InspectAsync(stream, selected.FileName);

            var length = info.Length is null
                ? "ukuran tidak tersedia"
                : $"{info.Length:N0} byte";
            var kind = info.Kind == SupportedFileKind.Unknown
                ? "Jenis file belum dikenali"
                : info.Kind.ToString();

            _fileSummary.Text = $"{info.Name}\n{kind} · {length}\nWorkspace copy: {Path.GetFileName(workspacePath)}";
            _headerPreview.Text = info.HeaderHex.Length == 0
                ? "File kosong."
                : $"HEX: {info.HeaderHex}\nASCII: {info.HeaderText}";
            _status.Text = "File berhasil disalin ke workspace privat aplikasi dan dibaca untuk inspeksi awal. Parser dan fungsi edit belum diaktifkan.";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gagal membuka file", ex.Message, "OK");
        }
    }
}
