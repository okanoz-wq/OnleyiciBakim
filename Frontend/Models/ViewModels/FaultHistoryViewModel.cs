namespace OnleyiciBakimSistemi.Models.ViewModels;

public class FaultHistoryViewModel
{
    public List<FaultRecord> Faults { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public List<Machine> Machines { get; set; } = new();
    public List<string> FaultTypes { get; set; } = new();
    public List<string> Severities { get; set; } = new();

    public string? SelectedMachineId { get; set; }
    public string SelectedFaultType { get; set; } = "Tümü";
    public string SelectedSeverity { get; set; } = "Tümü";
}
