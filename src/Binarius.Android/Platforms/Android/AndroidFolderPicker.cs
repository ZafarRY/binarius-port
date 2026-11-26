
using Android.App;
using Android.Content;
using AndroidUri = global::Android.Net.Uri;
using Microsoft.Maui.ApplicationModel;

namespace Binarius.Android;

public static class AndroidFolderPicker
{
    private const int RequestCode = 9127;

    private static TaskCompletionSource<AndroidUri?>? _completion;

    public static Task<AndroidUri?> PickAsync(string title)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException(
                "Android Activity unavailable.");

        if (_completion is not null)
            throw new InvalidOperationException(
                "A folder picker is already active.");

        _completion = new TaskCompletionSource<AndroidUri?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var intent = new Intent(Intent.ActionOpenDocumentTree);

        intent.PutExtra(Intent.ExtraTitle, title);

        intent.AddFlags(
            ActivityFlags.GrantReadUriPermission |
            ActivityFlags.GrantWriteUriPermission |
            ActivityFlags.GrantPersistableUriPermission);

        activity.StartActivityForResult(intent, RequestCode);

        return _completion.Task;
    }

    internal static void Complete(
        int requestCode,
        global::Android.App.Result resultCode,
        Intent? data)
    {
        if (requestCode != RequestCode || _completion is null)
            return;

        var completion = _completion;
        _completion = null;

        if (resultCode != global::Android.App.Result.Ok ||
            data?.Data is null)
        {
            completion.TrySetResult(null);
            return;
        }

        var uri = data.Data;

        var flags = data.Flags &
            (ActivityFlags.GrantReadUriPermission |
             ActivityFlags.GrantWriteUriPermission);

        try
        {
            Platform.CurrentActivity?.ContentResolver?
                .TakePersistableUriPermission(uri, flags);
        }
        catch
        {
            // Keep the current session URI if persistence fails.
        }

        completion.TrySetResult(uri);
    }
}
