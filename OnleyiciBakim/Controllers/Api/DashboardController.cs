using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromQuery] int months = 6,
        [FromQuery] int riskyMachineCount = 5,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (year is < 1900 or > 2200)
            return ValidationProblem("Yıl 1900-2200 aralığında olmalıdır.");
        return Ok(await dashboardService.GetAsync(
            months, riskyMachineCount, year, cancellationToken));
    }
}
