using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;
using OnleyiciBakim.Contracts.Workflows;
using OnleyiciBakim.Domain.Entities;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/alerts")]
public sealed class AlertsController(
    ApplicationDbContext dbContext,
    IRiskClassificationService classificationService,
    IWorkflowTransitionService transitions)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<AlertResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> GetAll(
        [FromQuery] AlertStatus? status,
        [FromQuery] string? machineId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = dbContext.Alerts.AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.GeneratedAt,
                x.MachineId,
                MachineName = x.Machine.Name,
                x.RiskAssessmentId,
                Score = x.RiskAssessment.HybridScore,
                x.Level,
                x.Title,
                x.Description,
                x.RecommendedAction,
                x.Status,
                x.DueAt,
                Personnel = x.ResponsiblePersonnel != null ? x.ResponsiblePersonnel.FullName : null
            })
            .ToListAsync(cancellationToken);
        var items = rows.Select(x => new AlertResponse(
            x.Id,
            x.GeneratedAt,
            x.MachineId,
            x.MachineName,
            x.RiskAssessmentId,
            new RiskPresentation(x.Score, classificationService.GetLabel(x.Level),
                classificationService.GetColorName(x.Level), classificationService.GetColorHex(x.Level)),
            x.Title,
            x.Description,
            x.RecommendedAction,
            x.Status.ToString(),
            x.DueAt,
            x.Personnel)).ToArray();
        return Ok(new PagedResponse<AlertResponse>(items, page, pageSize, total));
    }

    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Acknowledge(string id, CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli uyarı bulunamadı.");
        if (alert.Status == AlertStatus.Closed)
            throw new ConflictException("Çözümlenmiş uyarı tekrar işleme alınamaz.");
        transitions.EnsureAlertTransition(alert.Status, AlertStatus.Assigned);
        alert.Status = AlertStatus.Assigned;
        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Resolve(
        string id,
        ResolveAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli uyarı bulunamadı.");
        transitions.EnsureAlertTransition(alert.Status, AlertStatus.Closed);
        alert.Status = AlertStatus.Closed;
        alert.ResolvedAt = DateTimeOffset.UtcNow;
        alert.ResolutionNote = request.ResolutionNote;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Assign(
        string id,
        AssignAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli uyarı bulunamadı.");
        if (!await dbContext.Personnel.AnyAsync(x => x.Id == request.PersonnelId, cancellationToken))
            throw new ResourceNotFoundException($"'{request.PersonnelId}' kimlikli personel bulunamadı.");
        transitions.EnsureAlertTransition(alert.Status, AlertStatus.Assigned);
        alert.Status = AlertStatus.Assigned;
        alert.ResponsiblePersonnelId = request.PersonnelId;
        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/create-maintenance-plan")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMaintenancePlan(
        string id,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts
            .Include(x => x.RiskAssessment)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli uyarı bulunamadı.");
        transitions.EnsureAlertTransition(alert.Status, AlertStatus.ConvertedToPlan);
        var existing = await dbContext.MaintenancePlans
            .FirstOrDefaultAsync(x => x.RiskAssessmentId == alert.RiskAssessmentId, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Uyarının bağlı olduğu analizden '{existing.Id}' planı zaten üretildi.");
        var plan = new MaintenancePlan
        {
            Id = $"PLN-{Guid.NewGuid():N}"[..28],
            MachineId = alert.MachineId,
            PlannedAt = alert.DueAt ?? DateTimeOffset.UtcNow.AddDays(7),
            MaintenanceType = alert.Level == RiskLevel.Critical ? "Acil" : "Önleyici",
            Priority = classificationService.GetLabel(alert.Level),
            Status = MaintenancePlanStatus.Suggested,
            Justification = alert.Description,
            Recommendation = alert.RecommendedAction,
            RiskAssessmentId = alert.RiskAssessmentId,
            ModelConfidence = alert.RiskAssessment.ModelConfidence,
            DataSource = "EarlyWarning"
        };
        dbContext.MaintenancePlans.Add(plan);
        alert.Status = AlertStatus.ConvertedToPlan;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/maintenance/plans/{plan.Id}", new { plan.Id });
    }

    [HttpPost("{id}/no-action")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> NoAction(
        string id,
        NoActionRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli uyarı bulunamadı.");
        transitions.EnsureAlertTransition(alert.Status, AlertStatus.NoActionRequired);
        alert.Status = AlertStatus.NoActionRequired;
        alert.ResolutionNote = request.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
