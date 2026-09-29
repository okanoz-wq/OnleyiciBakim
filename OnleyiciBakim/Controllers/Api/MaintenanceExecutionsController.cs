using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Workflows;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/maintenance-executions")]
public sealed class MaintenanceExecutionsController(
    ApplicationDbContext dbContext,
    IWorkflowTransitionService transitions) : ControllerBase
{
    [HttpPost("plans/{planId}/start")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(
        string planId,
        StartMaintenanceExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.MaintenancePlans.FirstOrDefaultAsync(
            x => x.Id == planId, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{planId}' kimlikli bakım planı bulunamadı.");
        if (await dbContext.MaintenanceExecutions.AnyAsync(
                x => x.Id == request.ExecutionId, cancellationToken))
            throw new ConflictException($"'{request.ExecutionId}' kimlikli gerçekleştirme zaten var.");
        transitions.EnsureMaintenancePlanTransition(plan.Status, MaintenancePlanStatus.InProgress);
        plan.Status = MaintenancePlanStatus.InProgress;
        var execution = new MaintenanceExecution
        {
            Id = request.ExecutionId,
            MaintenancePlanId = planId,
            Status = MaintenanceExecutionStatus.InProgress,
            StartedAt = request.StartedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            PersonnelId = request.PersonnelId
        };
        dbContext.MaintenanceExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = execution.Id }, new { execution.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await dbContext.MaintenanceExecutions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.MaintenancePlanId, x.Status, x.StartedAt, x.CompletedAt,
                x.PersonnelId, x.Result, x.DurationMinutes, x.Cost
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli bakım gerçekleştirmesi bulunamadı.");
        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Complete(
        string id,
        CompleteMaintenanceExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var execution = await dbContext.MaintenanceExecutions
            .Include(x => x.MaintenancePlan)
            .ThenInclude(x => x.Machine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli bakım gerçekleştirmesi bulunamadı.");
        if (execution.Status != MaintenanceExecutionStatus.InProgress)
            throw new ConflictException("Yalnızca devam eden bakım tamamlanabilir.");
        transitions.EnsureMaintenancePlanTransition(
            execution.MaintenancePlan.Status, MaintenancePlanStatus.Completed);

        execution.Status = MaintenanceExecutionStatus.Completed;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.Result = request.Result;
        execution.DurationMinutes = request.DurationMinutes;
        execution.Cost = request.Cost;
        execution.MaintenancePlan.Status = MaintenancePlanStatus.Completed;
        execution.MaintenancePlan.Machine.LastMaintenanceAt = execution.CompletedAt;
        dbContext.MaintenanceExecutionChecklistResults.AddRange(
            request.ChecklistResults.Select(x => new MaintenanceExecutionChecklistResult
            {
                Id = $"CHK-{Guid.NewGuid():N}"[..28],
                MaintenanceExecutionId = id,
                ChecklistItemId = x.ChecklistItemId,
                IsPassed = x.IsPassed,
                Note = x.Note
            }));
        dbContext.ReplacedParts.AddRange(request.ReplacedParts.Select(x => new ReplacedPart
        {
            Id = $"PRT-{Guid.NewGuid():N}"[..28],
            MaintenanceExecutionId = id,
            PartCode = x.PartCode,
            PartName = x.PartName,
            Quantity = x.Quantity
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
