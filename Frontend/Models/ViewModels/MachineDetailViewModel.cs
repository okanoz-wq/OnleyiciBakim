namespace OnleyiciBakimSistemi.Models.ViewModels;

public class MachineDetailViewModel
{
    public Machine Machine { get; set; } = null!;
    public List<FaultRecord> Faults { get; set; } = new();
    public PaginationViewModel FaultPagination { get; set; } = new();
}
