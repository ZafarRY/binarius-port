using Binarius.Core.Models;
using Binarius.Core.Services;
using Microsoft.Maui.Storage;

namespace Binarius.Android;

internal static class KeyBookStore
{
    private static readonly Dictionary<NfsGame, KeyBook> Singles = new();
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static KeyBook? _all;

    /// <param name="game">A specific game, or null for all games merged.</param>
    public static async Task<KeyBook> GetAsync(NfsGame? game)
    {
        await Gate.WaitAsync();
        try
        {
            if (game is { } single)
                return await GetSingleAsync(single);

            if (_all is not null) return _all;

            var books = new List<KeyBook>();
            foreach (var each in Enum.GetValues<NfsGame>())
                books.Add(await GetSingleAsync(each));

            _all = KeyBook.Merge(books);
            return _all;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<KeyBook> GetSingleAsync(NfsGame game)
    {
        if (Singles.TryGetValue(game, out var cached)) return cached;

        await using var stream = await FileSystem.OpenAppPackageFileAsync(
            $"LegacyData/MainKeys/{KeyBook.FileName(game)}");
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        var book = await Task.Run(() => new KeyBook(text.Split('\n')));
        Singles[game] = book;
        return book;
    }
}