using Microsoft.Maui.Controls;

namespace Binarius.Android;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new NavigationPage(new IntroPage())
        {
            BarBackgroundColor = Color.FromArgb("#101318"),
            BarTextColor = Color.FromArgb("#F3F5F7")
        });
}