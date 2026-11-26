using Binarius.Core.Hashing;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace Binarius.Android;

internal static class ToolUi
{
    public static readonly Color Background = Color.FromArgb("#101318");
    public static readonly Color Surface = Color.FromArgb("#1A2029");
    public static readonly Color Stroke = Color.FromArgb("#303A47");
    public static readonly Color Primary = Color.FromArgb("#F3F5F7");
    public static readonly Color Muted = Color.FromArgb("#AAB4C0");
    public static readonly Color Warning = Color.FromArgb("#F2C45A");

    public static Label Caption(string text) => new()
    {
        Text = text,
        TextColor = Muted,
        FontSize = 13
    };

    public static Entry MonoEntry(bool readOnly) => new()
    {
        FontFamily = "monospace",
        FontSize = 15,
        TextColor = Primary,
        PlaceholderColor = Muted,
        BackgroundColor = Surface,
        IsReadOnly = readOnly,
        IsSpellCheckEnabled = false,
        IsTextPredictionEnabled = false
    };

    public static Button CopyButton(Func<string?> getText)
    {
        var button = new Button
        {
            Text = "Copy",
            WidthRequest = 88,
            HeightRequest = 44,
            CornerRadius = 10,
            BackgroundColor = Surface,
            TextColor = Primary,
            BorderColor = Stroke,
            BorderWidth = 1
        };

        button.Clicked += async (_, _) =>
        {
            var text = getText();
            if (string.IsNullOrEmpty(text)) return;

            await Clipboard.Default.SetTextAsync(text);
            button.Text = "Copied";
            await Task.Delay(900);
            button.Text = "Copy";
        };
        return button;
    }

    public static View Field(string caption, View input, Button? copy = null)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(Caption(caption));

        if (copy is null)
        {
            stack.Children.Add(input);
            return stack;
        }

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        row.Add(input, 0);
        row.Add(copy, 1);
        stack.Children.Add(row);
        return stack;
    }

    public static Label? UnverifiedNotice() =>
        NfsHash.IsVerified
            ? null
            : new Label
            {
                Text = "Hash algorithm not yet verified against the original Nikki source.",
                TextColor = Warning,
                FontSize = 12
            };
}