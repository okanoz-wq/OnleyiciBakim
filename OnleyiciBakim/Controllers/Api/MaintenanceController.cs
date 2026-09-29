using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Maintenance;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/maintenance")]
public sealed class MaintenanceController(
    ApplicationDbContext dbContext,
    IWorkflowTransitionService transitions) : ControllerBase
{
    [HttpGet("plans")]
    [ProducesResponseType<PagedResponse<MaintenancePlanResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MaintenancePlanResponse>>> GetPlans(
        [FromQuery] MaintenancePlanQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.MaintenancePlans.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.MachineId))
            query = query.Where(x => x.MachineId == request.MachineId);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.From.HasValue)
            query = query.Where(x => x.PlannedAt >= request.From.Value.ToUniversalTime());
        if (request.To.HasValue)
            query = query.Where(x => x.PlannedAt <= request.To.Value.ToUniversalTime());

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.PlannedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MaintenancePlanResponse(
                x.Id,
                x.MachineId,
                x.Machine.Name,
                x.PlannedAt,
                x.MaintenanceType,
                x.Priority,
                x.Status.ToString(),
                x.ModelConfidence,
                x.RiskAssessment != null
                    ? x.RiskAssessment.HybridScore
                    : x.Machine.CurrentRiskScore,
                x.Recommendation,
                x.Justification,
                x.ResponsiblePersonnel != null ? x.ResponsiblePersonnel.FullName : null,
                x.ErpWorkOrderNumber))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResponse<MaintenancePlanResponse>(
            items, request.Page, request.PageSize, total));
    }

    [HttpGet("calendar")]
    public Task<ActionResult<PagedResponse<MaintenancePlanResponse>>> GetCalendar(
        [FromQuery] MaintenancePlanQuery request,
        CancellationToken cancellationToken) => GetPlans(request, cancellationToken);

    [HttpPost("plans")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePlan(
        CreateMaintenancePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaintenancePlans.AnyAsync(x => x.Id == request.Id, cancellationToken))
            throw new ConflictException($"'{request.Id}' kimlikli bakım planı zaten kayıtlı.");
        if (!await dbContext.Machines.AnyAsync(x => x.Id == request.MachineId, cancellationToken))
            throw new ResourceNotFoundException($"'{request.MachineId}' kimlikli makine bulunamadı.");

        dbContext.MaintenancePlans.Add(new MaintenancePlan
        {
            Id = request.Id,
            MachineId = request.MachineId,
            ComponentCode = request.ComponentCode,
            ComponentName = request.ComponentName,
            PlannedAt = request.PlannedAt.ToUniversalTime(),
            MaintenanceType = request.MaintenanceType,
            Priority = request.Priority,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            EstimatedCost = request.EstimatedCost,
            ResponsiblePersonnelId = request.ResponsiblePersonnelId,
            Justification = request.Justification,
            ModelConfidence = request.ModelConfidence,
            Recommendation = request.Recommendation,
            RiskAssessmentId = request.RiskAssessmentId,
            ErpWorkOrderNumber = request.ErpWorkOrderNumber,
            Status = MaintenancePlanStatus.Draft
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPlans), new { id = request.Id }, new { request.Id });
    }

    [HttpPatch("plans/{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePlanStatus(
        string id,
        UpdateMaintenancePlanStatusRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.MaintenancePlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli bakım planı bulunamadı.");
        transitions.EnsureMaintenancePlanTransition(plan.Status, request.Status);
        plan.Status = request.Status;
        plan.ErpWorkOrderNumber = request.ErpWorkOrderNumber ?? plan.ErpWorkOrderNumber;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("plans/{id}/complete")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CompletePlan(
        string id,
        CompleteMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.MaintenancePlans
            .Include(x => x.Machine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli bakım planı bulunamadı.");
        if (await dbContext.MaintenanceRecords.AnyAsync(x => x.Id == request.MaintenanceRecordId, cancellationToken))
            throw new ConflictException($"'{request.MaintenanceRecordId}' kimlikli bakım kaydı zaten mevcut.");

        if (plan.Status != MaintenancePlanStatus.InProgress)
            throw new ConflictException("Bakım tamamlanmadan önce başlatılmalıdır.");
        var record = new MaintenanceRecord
        {
            Id = request.MaintenanceRecordId,
            MachineId = plan.MachineId,
            ComponentCode = plan.ComponentCode,
            ComponentName = plan.ComponentName,
            PerformedAt = request.PerformedAt.ToUniversalTime(),
            MaintenanceType = plan.MaintenanceType,
            Reason = plan.Justification,
            DurationMinutes = request.DurationMinutes,
            Result = request.Result,
            PersonnelId = request.PersonnelId ?? plan.ResponsiblePersonnelId,
            Cost = request.Cost,
            NextMaintenanceAt = request.NextMaintenanceAt?.ToUniversalTime(),
            WorkOrderNumber = plan.ErpWorkOrderNumber,
            ChecklistResultJson = request.ChecklistResults is null
                ? null
                : JsonSerializer.Serialize(request.ChecklistResults),
            DataSource = "Bakım gerçekleştirme"
        };
        dbContext.MaintenanceRecords.Add(record);
        plan.Status = Domain.Enums.MaintenancePlanStatus.Completed;
        plan.Machine.LastMaintenanceAt = record.PerformedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetHistory), new { machineId = plan.MachineId }, new { record.Id });
    }

    [HttpPost("plans/from-analysis/{analysisId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFromAnalysis(
        string analysisId,
        CancellationToken cancellationToken)
    {
        var analysis = await dbContext.RiskAssessments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == analysisId, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{analysisId}' kimlikli analiz bulunamadı.");
        var existing = await dbContext.MaintenancePlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RiskAssessmentId == analysisId, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Bu analizden '{existing.Id}' planı zaten oluşturulmuş.");
        if (analysis.IsFallback)
            throw new ConflictException("Fallback analizinden otomatik bakım planı oluşturulamaz.");

        var plan = new MaintenancePlan
        {
            Id = $"PLN-{Guid.NewGuid():N}"[..28],
            MachineId = analysis.MachineId,
            PlannedAt = analysis.EvaluatedAt.AddDays(analysis.RiskLevel switch
            {
                RiskLevel.Critical => 7,
                RiskLevel.High => 15,
                RiskLevel.Medium => 30,
                _ => 60
            }),
            MaintenanceType = analysis.RiskLevel >= RiskLevel.High ? "Acil" : "Planlı",
            Priority = analysis.RiskLevel.ToString(),
            Status = MaintenancePlanStatus.Suggested,
            Justification = analysis.RiskReasons,
            Recommendation = analysis.RecommendedAction,
            ModelConfidence = analysis.ModelConfidence,
            RiskAssessmentId = analysis.Id,
            DataSource = "RiskAnalysis"
        };
        dbContext.MaintenancePlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, new { plan.Id });
    }

    [HttpPost("plans/{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApprovePlan(string id, CancellationToken cancellationToken)
    {
        var plan = await dbContext.MaintenancePlans.FirstOrDefaultAsync(
            x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli bakım planı bulunamadı.");
        transitions.EnsureMaintenancePlanTransition(plan.Status, MaintenancePlanStatus.Approved);
        plan.Status = MaintenancePlanStatus.Approved;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("history")]
    [ProducesResponseType<PagedResponse<MaintenanceHistoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MaintenanceHistoryResponse>>> GetHistory(
        [FromQuery] string? machineId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = dbContext.MaintenanceRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(machineId))
            query = query.Where(x => x.MachineId == machineId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MaintenanceHistoryResponse(
                x.Id,
                x.MachineId,
                x.Machine.Name,
                x.PerformedAt,
                x.MaintenanceType,
                x.DurationMinutes,
                x.Status,
                x.Personnel != null ? x.Personnel.FullName : null,
                x.Cost,
                x.Result,
                x.NextMaintenanceAt))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResponse<MaintenanceHistoryResponse>(items, page, pageSize, total));
    }
}
