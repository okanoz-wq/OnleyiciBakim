using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Failures;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Contracts.Workflows;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/failures")]
public sealed class FailuresController(
    ApplicationDbContext dbContext,
    IWorkflowTransitionService transitions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<FailureResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<FailureResponse>>> GetAll(
        [FromQuery] FailureQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FailureRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.MachineId))
            query = query.Where(x => x.MachineId == request.MachineId);
        if (!string.IsNullOrWhiteSpace(request.FailureType))
            query = query.Where(x => x.FailureType == request.FailureType);
        if (!string.IsNullOrWhiteSpace(request.Severity))
            query = query.Where(x => x.Severity == request.Severity);
        if (Enum.TryParse<RecordStatus>(request.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (request.From.HasValue)
            query = query.Where(x => x.OccurredAt >= request.From.Value.ToUniversalTime());
        if (request.To.HasValue)
            query = query.Where(x => x.OccurredAt <= request.To.Value.ToUniversalTime());

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new FailureResponse(
                x.Id,
                x.OccurredAt,
                x.MachineId,
                x.Machine.Name,
                x.FailureType,
                x.Severity,
                Math.Round((x.DowntimeMinutes ?? 0) / 60m, 1),
                x.Personnel != null ? x.Personnel.FullName : null,
                x.Status.ToString(),
                x.RootCause,
                x.WorkOrderNumber,
                x.EstimatedCost,
                x.ClosedAt))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResponse<FailureResponse>(items, request.Page, request.PageSize, total));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<FailureResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FailureResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.FailureRecords
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new FailureResponse(
                x.Id,
                x.OccurredAt,
                x.MachineId,
                x.Machine.Name,
                x.FailureType,
                x.Severity,
                Math.Round((x.DowntimeMinutes ?? 0) / 60m, 1),
                x.Personnel != null ? x.Personnel.FullName : null,
                x.Status.ToString(),
                x.RootCause,
                x.WorkOrderNumber,
                x.EstimatedCost,
                x.ClosedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli arıza kaydı bulunamadı.");
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateFailureRequest request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.FailureRecords.AnyAsync(x => x.Id == request.Id, cancellationToken))
            throw new ConflictException($"'{request.Id}' kimlikli arıza zaten kayıtlı.");
        var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.Id == request.MachineId, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{request.MachineId}' kimlikli makine bulunamadı.");

        var failure = new FailureRecord
        {
            Id = request.Id,
            OccurredAt = request.OccurredAt.ToUniversalTime(),
            MachineId = request.MachineId,
            ComponentCode = request.ComponentCode,
            ComponentName = request.ComponentName,
            FailureType = request.FailureType,
            Severity = request.Severity,
            IsRecurring = request.IsRecurring,
            IsPlannedDowntime = request.IsPlannedDowntime,
            FirstResponseMinutes = request.FirstResponseMinutes,
            InterventionMinutes = request.InterventionMinutes,
            DowntimeMinutes = request.DowntimeMinutes,
            RootCause = request.RootCause,
            Description = request.Description,
            PersonnelId = request.PersonnelId,
            WorkOrderNumber = request.WorkOrderNumber,
            EstimatedCost = request.EstimatedCost,
            DataSource = request.DataSource
        };
        dbContext.FailureRecords.Add(failure);
        if (!machine.LastFailureAt.HasValue || machine.LastFailureAt < failure.OccurredAt)
            machine.LastFailureAt = failure.OccurredAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = failure.Id }, new { failure.Id });
    }

    [HttpPost("{id}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Close(
        string id,
        CloseFailureRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await dbContext.FailureRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli arıza kaydı bulunamadı.");
        if (failure.Status == RecordStatus.Open)
            throw new ConflictException("Arıza kapatılmadan önce müdahale başlatılmalıdır.");
        if (failure.Status == RecordStatus.InProgress)
        {
            transitions.EnsureFailureTransition(failure.Status, RecordStatus.Resolved);
            failure.Status = RecordStatus.Resolved;
        }
        transitions.EnsureFailureTransition(failure.Status, RecordStatus.Closed);

        failure.Status = RecordStatus.Closed;
        failure.ClosedAt = request.ClosedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        failure.Resolution = request.Resolution;
        failure.RootCause = request.RootCause ?? failure.RootCause;
        failure.PersonnelId = request.PersonnelId ?? failure.PersonnelId;
        failure.InterventionMinutes = request.InterventionMinutes ?? failure.InterventionMinutes;
        failure.DowntimeMinutes = request.DowntimeMinutes ?? failure.DowntimeMinutes;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/interventions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddIntervention(
        string id,
        AddFailureInterventionRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await dbContext.FailureRecords.FirstOrDefaultAsync(
            x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli arıza kaydı bulunamadı.");
        if (failure.Status is RecordStatus.Closed or RecordStatus.Cancelled)
            throw new ConflictException("Kapalı veya iptal edilmiş arızaya müdahale eklenemez.");
        if (await dbContext.FailureInterventions.AnyAsync(
                x => x.Id == request.Id, cancellationToken))
            throw new ConflictException($"'{request.Id}' kimlikli müdahale zaten kayıtlı.");

        if (failure.Status == RecordStatus.Open)
        {
            transitions.EnsureFailureTransition(failure.Status, RecordStatus.InProgress);
            failure.Status = RecordStatus.InProgress;
        }

        dbContext.FailureInterventions.Add(new FailureIntervention
        {
            Id = request.Id,
            FailureRecordId = id,
            StartedAt = request.StartedAt.ToUniversalTime(),
            EndedAt = request.EndedAt?.ToUniversalTime(),
            PersonnelId = request.PersonnelId,
            Note = request.Note
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { request.Id });
    }
}
