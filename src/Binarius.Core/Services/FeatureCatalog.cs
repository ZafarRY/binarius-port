using Binarius.Core.Models;

namespace Binarius.Core.Services;

/// <summary>
/// Inventory of the desktop features identified in the supplied Binarius source.
/// State remains Planned until each feature has been ported and tested.
/// </summary>
public static class FeatureCatalog
{
    public static IReadOnlyList<FeatureDefinition> All { get; } =
    [
        new("BIN / BUN / LZC Workspace", "Open, inspect, edit, and save Need for Speed binary data.", PortingState.Planned),
        new("Database / Collection Editor", "Browse managers, collections, expandable nodes, and properties.", PortingState.Planned),
        new("Endscript", "Parse and apply .end scripts with interactive choices and error reporting.", PortingState.Planned),
        new("Import / Export", "Import and export collections and create or restore backups.", PortingState.Planned),
        new("Texture Editor", "View and replace texture assets using Android-compatible image codecs.", PortingState.Planned),
        new("String Editor", "Find, add, update, replace, and remove string entries.", PortingState.Planned),
        new("Vector Editor", "Inspect and edit vector/vinyl data, including SVG import and export.", PortingState.Planned),
        new("Car Parts Editor", "Manage car-part collections, attributes, ordering, and search.", PortingState.Planned),
        new("Career Editor", "Inspect and edit career data and collections.", PortingState.Planned),
        new("NFS Hasher", "Calculate and display supported NFS hash values.", PortingState.Planned),
        new("NFS Raider", "Resolve hash values against the bundled game key dictionaries.", PortingState.Planned),
        new("Swatch Picker", "RGB-to-paint-swatch calculation ported from the desktop source; validation pending.", PortingState.SourcePortedPendingValidation),
        new("LAN Maker", "Port the desktop LAN/profile generation workflow after dependency review.", PortingState.Planned),
        new("Backup / Restore", "Create and restore backups of game database files using Android app storage.", PortingState.Planned),
        new("Themes & Settings", "Move desktop theme and settings workflows to MAUI Graphics and Android preferences.", PortingState.Planned)
    ];
}
