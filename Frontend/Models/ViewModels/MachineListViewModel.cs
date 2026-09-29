namespace OnleyiciBakimSistemi.Models.ViewModels;

public sealed class MachineListViewModel
{
    public List<ManagedMachineItemViewModel> Machines { get; set; } = [];
    public List<string> Statuses { get; set; } = [];
    public List<string> Criticalities { get; set; } = [];
    public List<string> Sources { get; set; } = ["ERP", "Local"];
    public string? Search { get; set; }
    public string? SelectedStatus { get; set; }
    public string? SelectedCriticality { get; set; }
    public string? SelectedSource { get; set; }
    public string SortBy { get; set; } = "risk";
    public bool Descending { get; set; } = true;
    public bool UseErpAsMasterSource { get; set; }
    public PaginationViewModel Pagination { get; set; } = new();
}
