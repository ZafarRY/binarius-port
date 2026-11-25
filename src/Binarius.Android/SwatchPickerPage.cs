using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using System.Globalization;
using Binarius.Core.Services;

namespace Binarius.Android;

public sealed class SwatchPickerPage : ContentPage
{
    private readonly Slider _red = CreateSlider();
    private readonly Slider _green = CreateSlider();
    private readonly Slider _blue = CreateSlider();
    private readonly BoxView _preview = new() { HeightRequest = 72, CornerRadius = 12 };
    private readonly Label _redValue = CreateValueLabel();
    private readonly Label _greenValue = CreateValueLabel();
    private readonly Label _blueValue = CreateValueLabel();
    private readonly Label _paintSwatch = CreateResultLabel();
    private readonly Label _saturation = CreateResultLabel();
    private readonly Label _brightness = CreateResultLabel();

    public SwatchPickerPage()
    {
        Title = "Swatch Picker";
        BackgroundColor = Color.FromArgb("#101318");

        _red.Value = 128;
        _green.Value = 128;
        _blue.Value = 128;
        _red.ValueChanged += OnRgbChanged;
        _green.ValueChanged += OnRgbChanged;
        _blue.ValueChanged += OnRgbChanged;

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 24),
            Spacing = 14
        };

        content.Children.Add(new Label
        {
            Text = "RGB → NFS PAINT SWATCH",
            TextColor = Color.FromArgb("#65D6AD"),
            FontSize = 12,
            CharacterSpacing = 1.4,
            FontAttributes = FontAttributes.Bold
        });
        content.Children.Add(new Label
        {
            Text = "Atur nilai RGB untuk mendapatkan paint swatch, saturation, dan brightness.",
            TextColor = Color.FromArgb("#AAB4C0"),
            FontSize = 13
        });
        content.Children.Add(_preview);
        content.Children.Add(CreateSliderRow("Red", _red, _redValue));
        content.Children.Add(CreateSliderRow("Green", _green, _greenValue));
        content.Children.Add(CreateSliderRow("Blue", _blue, _blueValue));
        content.Children.Add(CreateResultRow("Paint Swatch", _paintSwatch));
        content.Children.Add(CreateResultRow("Saturation", _saturation));
        content.Children.Add(CreateResultRow("Brightness", _brightness));

        var reset = new Button
        {
            Text = "Reset RGB",
            BackgroundColor = Color.FromArgb("#303A47"),
            TextColor = Color.FromArgb("#F3F5F7"),
            CornerRadius = 10
        };
        reset.Clicked += (_, _) =>
        {
            _red.Value = 128;
            _green.Value = 128;
            _blue.Value = 128;
            UpdateResult();
        };
        content.Children.Add(reset);

        Content = new ScrollView { Content = content };
        UpdateResult();
    }

    private static Slider CreateSlider() => new()
    {
        Minimum = 0,
        Maximum = 255,
        Value = 128
    };

    private static Label CreateValueLabel() => new()
    {
        TextColor = Color.FromArgb("#F3F5F7"),
        FontSize = 13,
        HorizontalTextAlignment = TextAlignment.End,
        WidthRequest = 42
    };

    private static Label CreateResultLabel() => new()
    {
        TextColor = Color.FromArgb("#65D6AD"),
        FontSize = 18,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.End
    };

    private static View CreateSliderRow(string name, Slider slider, Label value)
    {
        var layout = new Grid
        {
            ColumnSpacing = 10,
            VerticalOptions = LayoutOptions.Center
        };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        var title = new Label
        {
            Text = name,
            TextColor = Color.FromArgb("#F3F5F7"),
            VerticalTextAlignment = TextAlignment.Center,
            WidthRequest = 54
        };
        Grid.SetColumn(title, 0);
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(value, 2);
        layout.Children.Add(title);
        layout.Children.Add(slider);
        layout.Children.Add(value);
        return layout;
    }

    private static Border CreateResultRow(string name, Label value)
    {
        var layout = new Grid
        {
            Padding = 12
        };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nameLabel = new Label
        {
            Text = name,
            TextColor = Color.FromArgb("#AAB4C0"),
            VerticalTextAlignment = TextAlignment.Center
        };
        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(value, 1);
        layout.Children.Add(nameLabel);
        layout.Children.Add(value);

        var copy = new Button
        {
            Text = "Copy",
            BackgroundColor = Color.FromArgb("#303A47"),
            TextColor = Color.FromArgb("#F3F5F7"),
            CornerRadius = 8,
            Padding = new Thickness(10, 4)
        };
        copy.Clicked += async (_, _) => await Clipboard.Default.SetTextAsync(value.Text ?? string.Empty);

        var outer = new VerticalStackLayout { Spacing = 6 };
        outer.Children.Add(layout);
        outer.Children.Add(copy);
        return new Border
        {
            Stroke = Color.FromArgb("#303A47"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#1A2029"),
            Content = outer
        };
    }

    private void OnRgbChanged(object? sender, ValueChangedEventArgs e) => UpdateResult();

    private void UpdateResult()
    {
        var red = (byte)Math.Round(_red.Value, MidpointRounding.AwayFromZero);
        var green = (byte)Math.Round(_green.Value, MidpointRounding.AwayFromZero);
        var blue = (byte)Math.Round(_blue.Value, MidpointRounding.AwayFromZero);

        _redValue.Text = red.ToString(CultureInfo.InvariantCulture);
        _greenValue.Text = green.ToString(CultureInfo.InvariantCulture);
        _blueValue.Text = blue.ToString(CultureInfo.InvariantCulture);
        _preview.Color = Color.FromArgb($"#{red:X2}{green:X2}{blue:X2}");

        var result = PaintSwatchConverter.Convert(red, green, blue);
        _paintSwatch.Text = result.PaintSwatch.ToString(CultureInfo.InvariantCulture);
        _saturation.Text = result.Saturation.ToString(CultureInfo.InvariantCulture);
        _brightness.Text = result.Brightness.ToString(CultureInfo.InvariantCulture);
    }
}
