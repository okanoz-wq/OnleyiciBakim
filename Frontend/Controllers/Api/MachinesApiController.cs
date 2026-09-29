using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Services;

namespace OnleyiciBakimSistemi.Controllers.Api;

[ApiController]
[Route("api/machines")]
public class MachinesApiController : ControllerBase
{
    private readonly IMaintenanceDataService _data;

    public MachinesApiController(IMaintenanceDataService data)
    {
        _data = data;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _data.GetMachinesAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(string id, CancellationToken cancellationToken)
    {
        var machine = await _data.GetMachineAsync(id, cancellationToken);
        return machine is null ? NotFound() : Ok(machine);
    }
}
