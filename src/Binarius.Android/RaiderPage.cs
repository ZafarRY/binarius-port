using Binarius.Core.Hashing;
using Binarius.Core.Models;
using Binarius.Core.Services;
using Microsoft.Maui.Controls;

namespace Binarius.Android;

public sealed class RaiderPage : ContentPage
{
    private static readonly string[] GameNames =
        ["All games", "Underground 1", "Underground 2", "Most Wanted", "Carbon", "ProStreet", "Undercover"];

    private readonly Picker _mode = new() { Title = "Choose search mode", TextColor = ToolUi.Primary, TitleColor = ToolUi.Muted };
    private readonly Picker _game = new() { TextColor = ToolUi.Primary };
    private readonly Entry _binMemory = ToolUi.MonoEntry(readOnly: true);
    private readonly Entry _binFile = ToolUi.MonoEntry(readOnly: true);
    private readonly Entry _guess = ToolUi.MonoEntry(readOnly: true);
    private readonly Label _status = ToolUi.Caption("");

    private KeyBook? _keys;
    private int _loadTicket;

    public RaiderPage()
    {
        Title = "NFS-Raider by MaxHwoy";
        BackgroundColor = ToolUi.Background;

        _mode.Items.Add("Use Bin Memory Hash search");
        _mode.Items.Add("Use Bin File Hash search");
        _mode.SelectedIndexChanged += OnModeChanged;

        foreach (var name in GameNames) _game.Items.Add(name);
        _game.SelectedIndex = 0;
        _game.SelectedIndexChanged += async (_, _) => await LoadKeysAsync();

        _binMemory.TextChanged += (_, e) => ApplyMemory(e.NewTextValue);
        _binFile.TextChanged += (_, e) => ApplyFile(e.NewTextValue);

        var content = new VerticalStackLayout { Padding = new Thickness(20), Spacing = 14 };
        if (ToolUi.UnverifiedNotice() is { } notice) content.Children.Add(notice);

        content.Children.Add(ToolUi.Field("Game", _game));
        content.Children.Add(_status);
        content.Children.Add(ToolUi.Field("Lookup", _mode));
        content.Children.Add(ToolUi.Field("Bin Memory", _binMemory, ToolUi.CopyButton(() => _binMemory.Text)));
        content.Children.Add(ToolUi.Field("Bin File", _binFile, ToolUi.CopyButton(() => _binFile.Text)));
        content.Children.Add(ToolUi.Field("String", _guess, ToolUi.CopyButton(() => _guess.Text)));

        Content = new ScrollView { Content = content };

        _ = LoadKeysAsync();
    }

    private async Task LoadKeysAsync()
    {
        var ticket = ++_loadTicket;
        NfsGame? game = _game.SelectedIndex <= 0 ? null : (NfsGame)(_game.SelectedIndex - 1);
        _status.Text = "Loading keys...";

        KeyBook? loaded = null;
        string status;
        try
        {
            loaded = await KeyBookStore.GetAsync(game);
            status = $"{loaded.Count:N0} keys loaded";
        }
        catch (Exception ex)
        {
            status = $"Could not load keys: {ex.Message}";
        }

        if (ticket != _loadTicket) return; // a newer selection superseded this one

        _keys = loaded;
        _status.Text = status;
        Recompute();
    }

    private void OnModeChanged(object? sender, EventArgs e)
    {
        if (_mode.SelectedIndex == 0)
        {
            _binMemory.IsReadOnly = false;
            _binFile.IsReadOnly = true;
        }
        else
        {
            _binMemory.IsReadOnly = true;
            _binFile.IsReadOnly = false;
        }
    }

    private void ApplyMemory(string? text)
    {
        if (_binMemory.IsReadOnly) return;
        if (!HexKey.TryParse(text, out var key, out var tooLong)) return;
        if (tooLong) { _guess.Text = "N/A"; return; }

        _binFile.Text = $"0x{NfsHash.Reverse(key):X8}";
        _guess.Text = Lookup(key);
    }

    private void ApplyFile(string? text)
    {
        if (_binFile.IsReadOnly) return;
        if (!HexKey.TryParse(text, out var key, out var tooLong)) return;
        if (tooLong) { _guess.Text = "N/A"; return; }

        key = NfsHash.Reverse(key);
        _binMemory.Text = $"0x{key:X8}";
        _guess.Text = Lookup(key);
    }

    private void Recompute()
    {
        ApplyMemory(_binMemory.Text);
        ApplyFile(_binFile.Text);
    }

    private string Lookup(uint key) =>
        _keys is not null && _keys.TryGetValue(key, out var name) ? name : "N/A";
}