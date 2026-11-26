
using Android.App;
using Android.Content;
using Android.Net;
using Microsoft.Maui.ApplicationModel;

namespace Binarius.Android;

public static class AndroidFolderPicker
{
    private const int RequestCode = 9127;

    private static TaskCompletionSource<Uri?>? _completion;

    public static Task<Uri?> PickAsync(string title)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("Android Activity unavailable.");

        if (_completion is not null)
            throw new InvalidOperationException("A folder picker is already active.");

        _completion = new TaskCompletionSource<Uri?>(
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
        Result resultCode,
        Intent? data)
    {
        if (requestCode != RequestCode || _completion is null)
            return;

        var completion = _completion;
        _completion = null;

        if (resultCode != Result.Ok || data?.Data is null)
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
            // The returned URI remains usable for the current session.
        }

        completion.TrySetResult(uri);
    }
}
