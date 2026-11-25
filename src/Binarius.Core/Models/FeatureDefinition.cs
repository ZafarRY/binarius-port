namespace Binarius.Core.Models;

public enum PortingState
{
    Planned,
    InProgress,
    Implemented,
    SourcePortedPendingValidation,
    Blocked
}

public sealed record FeatureDefinition(
    string Name,
    string Description,
    PortingState State)
{
    public string StateLabel => State switch
    {
        PortingState.Planned => "Belum dipindahkan",
        PortingState.InProgress => "Dalam pengerjaan",
        PortingState.Implemented => "Selesai dan tervalidasi",
        PortingState.SourcePortedPendingValidation => "Source ported; build/device test pending",
        PortingState.Blocked => "Menunggu dependency",
        _ => "Tidak diketahui"
    };
}
