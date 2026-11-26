using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace Binarius.Android;

public sealed class IntroPage : ContentPage
{
    private static readonly Color Accent = Color.FromArgb("#65D6AD");
    private static readonly Color Primary = Color.FromArgb("#F3F5F7");
    private static readonly Color Muted = Color.FromArgb("#AAB4C0");

    public IntroPage()
    {
        BackgroundColor = Color.FromArgb("#101318");
        NavigationPage.SetHasNavigationBar(this, false);

        var tools = SmallButton("Tools");
        tools.Clicked += OnToolsClicked;

        var update = SmallButton("Update");
        update.Clicked += async (_, _) =>
            await Launcher.Default.OpenAsync("https://github.com/nlgxzef/Binarius/releases");

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        header.Add(new Label
        {
            Text = "BINARIUS",
            TextColor = Accent,
            FontSize = 13,
            CharacterSpacing = 3,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        }, 0);
        header.Add(update, 1);
        header.Add(tools, 2);

        var content = new VerticalStackLayout { Padding = new Thickness(20, 28), Spacing = 16 };
        content.Children.Add(header);
        content.Children.Add(new Label
        {
            Text = "Pilih mode",
            TextColor = Primary,
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 12, 0, 4)
        });
        content.Children.Add(ModeCard("👤", "User Mode", "Install a mod",
            () => Navigation.PushAsync(new UserModePage())));
        content.Children.Add(ModeCard("🛠️", "Modder Mode", "Create a mod",
            () => Navigation.PushAsync(new ModderPage())));

        Content = new ScrollView { Content = content };
    }

    private static Button SmallButton(string text) => new()
    {
        Text = text,
        BackgroundColor = Color.FromArgb("#1A2029"),
        TextColor = Primary,
        CornerRadius = 10,
        HeightRequest = 38,
        Padding = new Thickness(14, 0)
    };

    private static Border ModeCard(string icon, string title, string description, Func<Task> onTap)
    {
        var text = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        text.Children.Add(new Label
        {
            Text = title,
            TextColor = Primary,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        });
        text.Children.Add(new Label { Text = description, TextColor = Muted, FontSize = 13 });

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 16
        };
        row.Add(new Label { Text = icon, FontSize = 40, VerticalOptions = LayoutOptions.Center }, 0);
        row.Add(text, 1);

        var card = new Border
        {
            Stroke = Color.FromArgb("#303A47"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Color.FromArgb("#1A2029"),
            Padding = 20,
            Content = row
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await onTap();
        card.GestureRecognizers.Add(tap);
        return card;
    }

    private async void OnToolsClicked(object? sender, EventArgs e)
    {
        var choice = await DisplayActionSheetAsync(
            "Tools", "Batal", null,
            "New Launcher", "Hasher", "Raider", "Swatcher", "Settings");

        switch (choice)
        {
            case "Swatcher":
                await Navigation.PushAsync(new SwatchPickerPage());
                break;
            case "Hasher":
                await Navigation.PushAsync(new HasherPage());
                break;
            case "Raider":
                await Navigation.PushAsync(new RaiderPage());
                break;
            case "New Launcher":
            case "Settings":
                await DisplayAlertAsync(choice, "Belum dipindahkan ke Android.", "OK");
                break;
        }
    }
}