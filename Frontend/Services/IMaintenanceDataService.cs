using OnleyiciBakimSistemi.Models;
using OnleyiciBakimSistemi.Models.ViewModels;

namespace OnleyiciBakimSistemi.Services;

public interface IMaintenanceDataService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Machine>> GetMachinesAsync(CancellationToken cancellationToken = default);
    Task<Machine?> GetMachineAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaultRecord>> GetFaultsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaultRecord>> GetFaultsByMachineAsync(
        string machineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenancePlanItem>> GetMaintenancePlansAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyFaultPoint>> GetMonthlyFaultTrendAsync(
        int months,
        int? year,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetFaultYearsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaultTypeDistributionPoint>> GetFaultTypeDistributionAsync(
        CancellationToken cancellationToken = default);
    IReadOnlyList<string> FaultTypes { get; }
    IReadOnlyList<string> Severities { get; }
}
