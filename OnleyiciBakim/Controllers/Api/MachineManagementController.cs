using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Services.Management;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/bakim-yonetimi/management/machines")]
public sealed class MachineManagementController(
    IMachineManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ManagedMachineListItem>>> Get(
        [FromQuery] ManagedMachineQuery query,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(query, cancellationToken));

    [HttpGet("lookups")]
    public async Task<ActionResult<MachineManagementLookups>> GetLookups(
        CancellationToken cancellationToken) =>
        Ok(await service.GetLookupsAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<ManagedMachineDetails>> GetDetails(
        string id,
        CancellationToken cancellationToken) =>
        Ok(await service.GetDetailsAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ManagementOperationResult>> Create(
        MachineUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDetails), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ManagementOperationResult>> Update(
        string id,
        MachineUpsertRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    public async Task<ActionResult<ManagementOperationResult>> Deactivate(
        string id,
        CancellationToken cancellationToken) =>
        Ok(await service.DeactivateAsync(id, cancellationToken));

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ManagementOperationResult>> Activate(
        string id,
        CancellationToken cancellationToken) =>
        Ok(await service.ActivateAsync(id, cancellationToken));
}
