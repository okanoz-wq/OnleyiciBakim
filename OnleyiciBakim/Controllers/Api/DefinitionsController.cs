using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Definitions;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Infrastructure;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/definitions")]
public sealed class DefinitionsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("machine-components")]
    public async Task<IActionResult> GetMachineComponents(CancellationToken cancellationToken) =>
        Ok(await dbContext.MachineComponents.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken));

    [HttpPost("machine-components")]
    public async Task<IActionResult> CreateMachineComponent(
        CreateCodeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCodeIsAvailable(request.Id, request.Code, dbContext.MachineComponents, cancellationToken);
        dbContext.MachineComponents.Add(new MachineComponent
        {
            Id = request.Id,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/definitions/machine-components/{request.Id}", request);
    }

    [HttpGet("failure-codes")]
    public async Task<IActionResult> GetFailureCodes(CancellationToken cancellationToken) =>
        Ok(await dbContext.FailureCodes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken));

    [HttpPost("failure-codes")]
    public async Task<IActionResult> CreateFailureCode(
        CreateFailureCodeRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCodeIsAvailable(request.Id, request.Code, dbContext.FailureCodes, cancellationToken);
        dbContext.FailureCodes.Add(new FailureCode
        {
            Id = request.Id,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            DefaultSeverity = request.DefaultSeverity
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/definitions/failure-codes/{request.Id}", request);
    }

    [HttpGet("maintenance-types")]
    public async Task<IActionResult> GetMaintenanceTypes(CancellationToken cancellationToken) =>
        Ok(await dbContext.MaintenanceTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken));

    [HttpPost("maintenance-types")]
    public async Task<IActionResult> CreateMaintenanceType(
        CreateMaintenanceTypeRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCodeIsAvailable(request.Id, request.Code, dbContext.MaintenanceTypes, cancellationToken);
        dbContext.MaintenanceTypes.Add(new MaintenanceTypeDefinition
        {
            Id = request.Id,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            DefaultPeriodDays = request.DefaultPeriodDays
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/definitions/maintenance-types/{request.Id}", request);
    }

    [HttpGet("checklists")]
    public async Task<IActionResult> GetChecklists(CancellationToken cancellationToken) =>
        Ok(await dbContext.MaintenanceChecklists.AsNoTracking()
            .Include(x => x.Items.OrderBy(item => item.Order))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken));

    [HttpPost("checklists")]
    public async Task<IActionResult> CreateChecklist(
        CreateChecklistRequest request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaintenanceChecklists.AnyAsync(x => x.Id == request.Id, cancellationToken))
            throw new ConflictException($"'{request.Id}' kimlikli kontrol listesi zaten mevcut.");
        if (!await dbContext.MaintenanceTypes.AnyAsync(x => x.Id == request.MaintenanceTypeId, cancellationToken))
            throw new ResourceNotFoundException($"'{request.MaintenanceTypeId}' bakım türü bulunamadı.");

        var checklist = new MaintenanceChecklist
        {
            Id = request.Id,
            Name = request.Name,
            MachineType = request.MachineType,
            MaintenanceTypeId = request.MaintenanceTypeId,
            Items = request.Items.Select((item, index) => new MaintenanceChecklistItem
            {
                Order = index + 1,
                Description = item.Description,
                IsRequired = item.IsRequired
            }).ToList()
        };
        dbContext.MaintenanceChecklists.Add(checklist);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/definitions/checklists/{request.Id}", request);
    }

    [HttpGet("parameters")]
    public async Task<IActionResult> GetParameters(
        [FromQuery] string? group,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ApplicationParameters.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(group))
            query = query.Where(x => x.Group == group);
        return Ok(await query.OrderBy(x => x.Group).ThenBy(x => x.Key).ToListAsync(cancellationToken));
    }

    [HttpPut("parameters/{id}")]
    public async Task<IActionResult> UpsertParameter(
        string id,
        UpsertParameterRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(id, request.Id, StringComparison.Ordinal))
            throw new DomainValidationException("Adres kimliği ile istek kimliği aynı olmalıdır.");
        var parameter = await dbContext.ApplicationParameters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (parameter is null)
        {
            parameter = new ApplicationParameter { Id = id };
            dbContext.ApplicationParameters.Add(parameter);
        }
        parameter.Group = request.Group;
        parameter.Key = request.Key;
        parameter.Value = request.Value;
        parameter.DataType = request.DataType;
        parameter.Description = request.Description;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static async Task EnsureCodeIsAvailable<TEntity>(
        string id,
        string code,
        DbSet<TEntity> set,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var exists = await set.AsNoTracking().AnyAsync(
            x => EF.Property<string>(x, "Id") == id || EF.Property<string>(x, "Code") == code,
            cancellationToken);
        if (exists)
            throw new ConflictException("Aynı kimlik veya kodla tanım zaten mevcut.");
    }
}
