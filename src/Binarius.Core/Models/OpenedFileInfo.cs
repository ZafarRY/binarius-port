namespace Binarius.Core.Models;

public sealed record OpenedFileInfo(
    string Name,
    string Extension,
    SupportedFileKind Kind,
    long? Length,
    string HeaderHex,
    string HeaderText);
