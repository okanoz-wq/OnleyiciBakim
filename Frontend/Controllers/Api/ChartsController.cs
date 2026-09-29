using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Services;

namespace OnleyiciBakimSistemi.Controllers.Api;

// Web API controller: Dashboard sayfasındaki grafikler bu uçlardan JSON çekiyor.
// Aynı proje içinde MVC (View döndüren) ve API (JSON döndüren) controller'ların
// bir arada nasıl kullanıldığını gösteren örnek budur.
[ApiController]
[Route("api/charts")]
public class ChartsController : ControllerBase
{
    private readonly IMaintenanceDataService _data;

    public ChartsController(IMaintenanceDataService data)
    {
        _data = data;
    }

    [HttpGet("monthly-faults")]
    public async Task<IActionResult> MonthlyFaults(
        [FromQuery] int months = 6,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (year is < 1900 or > 2200)
            return ValidationProblem("Yıl 1900-2200 aralığında olmalıdır.");
        months = months == 12 ? 12 : 6;
        return Ok(await _data.GetMonthlyFaultTrendAsync(months, year, cancellationToken));
    }

    [HttpGet("failure-years")]
    public async Task<IActionResult> FailureYears(CancellationToken cancellationToken) =>
        Ok(await _data.GetFaultYearsAsync(cancellationToken));

    [HttpGet("fault-types")]
    public async Task<IActionResult> FaultTypes(CancellationToken cancellationToken) =>
        Ok(await _data.GetFaultTypeDistributionAsync(cancellationToken));
}
