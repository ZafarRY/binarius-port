using Microsoft.Maui;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;

namespace Binarius.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
  protected override void OnActivityResult(
      int requestCode,
      Android.App.Result resultCode,
      Intent? data)
  {
      base.OnActivityResult(requestCode, resultCode, data);
  
      AndroidFolderPicker.Complete(
          requestCode,
          resultCode,
          data);
  }
}
