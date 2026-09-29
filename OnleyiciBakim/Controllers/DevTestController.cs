using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers;

[Route("dev-test")]
public sealed class DevTestController(
    IHostEnvironment environment,
    IDevTestService devTestService) : Controller
{
    [HttpGet("")]
    [HttpGet("machines")]
    [HttpGet("failures")]
    [HttpGet("risk-analysis")]
    [HttpGet("maintenance")]
    [HttpGet("integrations")]
    [HttpGet("logs")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();
        return View(await devTestService.GetAsync(cancellationToken));
    }
}
