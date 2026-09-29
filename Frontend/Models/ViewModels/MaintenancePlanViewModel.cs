namespace OnleyiciBakimSistemi.Models.ViewModels;

public sealed class MaintenancePlanViewModel
{
    public List<MaintenancePlanItem> Plans { get; init; } = new();
    public PaginationViewModel Pagination { get; init; } = new();
}
