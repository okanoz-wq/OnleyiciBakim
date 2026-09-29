using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Models.ViewModels;
using OnleyiciBakimSistemi.Services;

namespace OnleyiciBakimSistemi.Controllers;

public class MaintenancePlanController : Controller
{
    private readonly IMaintenanceDataService _data;

    public MaintenancePlanController(IMaintenanceDataService data)
    {
        _data = data;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var plans = (await _data.GetMaintenancePlansAsync(cancellationToken)).ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(
            plans.Count / (double)PaginationViewModel.DefaultPageSize));
        page = Math.Clamp(page, 1, totalPages);

        return View(new MaintenancePlanViewModel
        {
            Plans = plans
                .Skip((page - 1) * PaginationViewModel.DefaultPageSize)
                .Take(PaginationViewModel.DefaultPageSize)
                .ToList(),
            Pagination = new PaginationViewModel
            {
                Controller = "MaintenancePlan",
                Page = page,
                TotalCount = plans.Count
            }
        });
    }
}
