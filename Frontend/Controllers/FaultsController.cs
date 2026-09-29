using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Models.ViewModels;
using OnleyiciBakimSistemi.Services;

namespace OnleyiciBakimSistemi.Controllers;

public class FaultsController : Controller
{
    private readonly IMaintenanceDataService _data;

    public FaultsController(IMaintenanceDataService data)
    {
        _data = data;
    }

    // GET /Faults?machineId=3&faultType=Elektrik&severity=Kritik
    public async Task<IActionResult> Index(
        string? machineId,
        string? faultType,
        string? severity,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var faults = (await _data.GetFaultsAsync(cancellationToken)).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(machineId))
        {
            faults = faults.Where(f => f.MachineId == machineId);
        }

        if (!string.IsNullOrWhiteSpace(faultType) && faultType != "Tümü")
        {
            faults = faults.Where(f => f.FaultType == faultType);
        }

        if (!string.IsNullOrWhiteSpace(severity) && severity != "Tümü")
        {
            faults = faults.Where(f => f.Severity == severity);
        }

        var filteredFaults = faults.OrderByDescending(f => f.Date).ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(
            filteredFaults.Count / (double)PaginationViewModel.DefaultPageSize));
        page = Math.Clamp(page, 1, totalPages);

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(machineId)) routeValues["machineId"] = machineId;
        if (!string.IsNullOrWhiteSpace(faultType)) routeValues["faultType"] = faultType;
        if (!string.IsNullOrWhiteSpace(severity)) routeValues["severity"] = severity;

        var vm = new FaultHistoryViewModel
        {
            Faults = filteredFaults
                .Skip((page - 1) * PaginationViewModel.DefaultPageSize)
                .Take(PaginationViewModel.DefaultPageSize)
                .ToList(),
            Machines = (await _data.GetMachinesAsync(cancellationToken)).ToList(),
            FaultTypes = _data.FaultTypes.ToList(),
            Severities = _data.Severities.ToList(),
            SelectedMachineId = machineId,
            Pagination = new PaginationViewModel
            {
                Controller = "Faults",
                Page = page,
                TotalCount = filteredFaults.Count,
                RouteValues = routeValues
            },
            SelectedFaultType = string.IsNullOrWhiteSpace(faultType) ? "Tümü" : faultType,
            SelectedSeverity = string.IsNullOrWhiteSpace(severity) ? "Tümü" : severity,
        };

        return View(vm);
    }
}
