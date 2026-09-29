namespace OnleyiciBakimSistemi.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalMachines { get; set; }
    public int RiskyMachineCount { get; set; }
    public int PendingPlanCount { get; set; }
    public double AverageDowntimeHours { get; set; }
    public List<Machine> TopRiskMachines { get; set; } = new();
}
