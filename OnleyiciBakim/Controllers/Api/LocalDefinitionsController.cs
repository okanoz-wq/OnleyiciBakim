using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Services.Management;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/local-definitions")]
public sealed class LocalDefinitionsController(ILocalDefinitionService service) : ControllerBase
{
    [HttpGet("modules")]
    public ActionResult<IReadOnlyList<LocalDefinitionMetadata>> Modules() => Ok(service.GetModules());

    [HttpGet("{module}")]
    public async Task<ActionResult<IReadOnlyList<LocalDefinitionRow>>> List(string module, [FromQuery] bool includeInactive, CancellationToken token) => Ok(await service.ListAsync(module, includeInactive, token));

    [HttpGet("{module}/{id}")]
    public async Task<ActionResult<LocalDefinitionRow>> Get(string module, string id, CancellationToken token) => Ok(await service.GetAsync(module, id, token));

    [HttpPost("{module}")]
    public async Task<ActionResult<LocalDefinitionRow>> Create(string module, [FromBody] LocalDefinitionUpsertRequest request, CancellationToken token)
    {
        var row = await service.SaveAsync(module, null, request, token);
        return CreatedAtAction(nameof(Get), new { module, id = row.Id }, row);
    }

    [HttpPut("{module}/{id}")]
    public async Task<ActionResult<LocalDefinitionRow>> Update(string module, string id, [FromBody] LocalDefinitionUpsertRequest request, CancellationToken token) => Ok(await service.SaveAsync(module, id, request, token));

    [HttpDelete("{module}/{id}")]
    public async Task<ActionResult> Deactivate(string module, string id, CancellationToken token)
    {
        await service.DeactivateAsync(module, id, token);
        return Ok(new { message = "Kayıt ilişkileri korunarak pasife alındı." });
    }
}
