using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Models.ViewModels;
using OnleyiciBakimSistemi.Services;

namespace OnleyiciBakimSistemi.Controllers;

public class HomeController : Controller
{
    private readonly IMaintenanceDataService _data;

    public HomeController(IMaintenanceDataService data)
    {
        _data = data;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _data.GetDashboardAsync(cancellationToken));
    }
}
