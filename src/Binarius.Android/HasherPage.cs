using Binarius.Core.Hashing;
using Microsoft.Maui.Controls;

namespace Binarius.Android;

public sealed class HasherPage : ContentPage
{
    private readonly Entry _input = ToolUi.MonoEntry(readOnly: false);
    private readonly Entry _binMemory = ToolUi.MonoEntry(readOnly: true);
    private readonly Entry _binFile = ToolUi.MonoEntry(readOnly: true);
    private readonly Entry _vltMemory = ToolUi.MonoEntry(readOnly: true);
    private readonly Entry _vltFile = ToolUi.MonoEntry(readOnly: true);

    public HasherPage()
    {
        Title = "NFS-Hasher by MaxHwoy";
        BackgroundColor = ToolUi.Background;

        _input.Placeholder = "Type a string";
        _input.TextChanged += (_, _) => Recalculate();

        var content = new VerticalStackLayout { Padding = new Thickness(20), Spacing = 14 };
        if (ToolUi.UnverifiedNotice() is { } notice) content.Children.Add(notice);

        content.Children.Add(ToolUi.Field("Input", _input, ToolUi.CopyButton(() => _input.Text)));
        content.Children.Add(ToolUi.Field("Bin Memory", _binMemory, ToolUi.CopyButton(() => _binMemory.Text)));
        content.Children.Add(ToolUi.Field("Bin File", _binFile, ToolUi.CopyButton(() => _binFile.Text)));
        content.Children.Add(ToolUi.Field("Vlt Memory", _vltMemory, ToolUi.CopyButton(() => _vltMemory.Text)));
        content.Children.Add(ToolUi.Field("Vlt File", _vltFile, ToolUi.CopyButton(() => _vltFile.Text)));

        Content = new ScrollView { Content = content };
    }

    private void Recalculate()
    {
        var text = _input.Text ?? string.Empty;

        var bin = NfsHash.BinHash(text);
        _binMemory.Text = $"0x{bin:X8}";
        _binFile.Text = $"0x{NfsHash.Reverse(bin):X8}";

        var vlt = NfsHash.VltHash(text);
        _vltMemory.Text = $"0x{vlt:X8}";
        _vltFile.Text = $"0x{NfsHash.Reverse(vlt):X8}";
    }
}