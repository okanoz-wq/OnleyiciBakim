using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Contracts.Integrations;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Integrations.Erp;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/integrations")]
[IntegrationApiKey]
public sealed class IntegrationsController(
    IIntegrationService integrationService,
    IRiskAssessmentService riskAssessmentService,
    IErpClient erpClient,
    ApplicationDbContext dbContext)
    : ControllerBase
{
    [HttpPost("erp/machines")]
    [ProducesResponseType<IntegrationResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationResult>> SyncMachines(
        ErpMachineSyncRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await integrationService.SyncMachinesAsync(request, cancellationToken));
    }

    [HttpPost("erp/failures")]
    [ProducesResponseType<IntegrationResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationResult>> SyncFailures(
        ErpFailureSyncRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await integrationService.SyncFailuresAsync(request, cancellationToken));
    }

    [HttpPost("ml/predictions")]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskAssessmentResponse>> ReceiveMlPrediction(
        MlPredictionCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await riskAssessmentService.EvaluateMachineAsync(
            request.MachineId,
            request,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("erp/pull-machines")]
    [ProducesResponseType<IntegrationResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationResult>> PullMachines(CancellationToken cancellationToken)
    {
        var machines = await erpClient.GetMachinesAsync(cancellationToken);
        var request = new ErpMachineSyncRequest
        {
            CorrelationId = $"erp-pull-{Guid.NewGuid():N}",
            Machines = machines
        };
        return Ok(await integrationService.SyncMachinesAsync(request, cancellationToken));
    }

    [HttpPost("erp/work-orders/{maintenancePlanId}")]
    public async Task<IActionResult> CreateWorkOrder(
        string maintenancePlanId,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.ErpWorkOrderMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaintenancePlanId == maintenancePlanId, cancellationToken);
        if (mapping is not null)
            return Ok(new { mapping.ErpWorkOrderId, mapping.Status, isIdempotentReplay = true });

        var plan = await dbContext.MaintenancePlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == maintenancePlanId, cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"'{maintenancePlanId}' kimlikli bakım planı bulunamadı.");
        var idempotencyKey = $"maintenance-plan:{maintenancePlanId}";
        var erpRequest = new ErpWorkOrderRequest(
            plan.Id, plan.MachineId, plan.PlannedAt, plan.MaintenanceType,
            plan.Priority, plan.Recommendation, idempotencyKey);
        var result = await erpClient.CreateWorkOrderAsync(erpRequest, cancellationToken);

        dbContext.IntegrationSyncLogs.Add(new IntegrationSyncLog
        {
            SystemName = "ERP",
            EntityType = "WorkOrder",
            ExternalId = result.ExternalWorkOrderId,
            Direction = SyncDirection.Outbound,
            Status = result.IsSuccess ? SyncStatus.Processed : SyncStatus.Pending,
            CorrelationId = idempotencyKey,
            OperationType = "CreateWorkOrder",
            PayloadJson = JsonSerializer.Serialize(new
            {
                plan.Id, plan.MachineId, plan.PlannedAt, plan.MaintenanceType, plan.Priority
            }),
            HttpStatusCode = result.HttpStatusCode,
            ErrorMessage = result.ErrorMessage
        });
        if (result.IsSuccess)
        {
            dbContext.ErpWorkOrderMappings.Add(new ErpWorkOrderMapping
            {
                Id = $"ERP-{Guid.NewGuid():N}"[..28],
                MaintenancePlanId = plan.Id,
                ErpWorkOrderId = result.ExternalWorkOrderId!,
                IdempotencyKey = idempotencyKey,
                Status = result.Status
            });
        }
        else
        {
            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "ErpCreateWorkOrder",
                PayloadJson = JsonSerializer.Serialize(erpRequest),
                IdempotencyKey = idempotencyKey,
                Status = SyncStatus.Pending,
                NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMinutes(1),
                LastError = result.ErrorMessage
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/integrations/erp/integration-logs", result)
            : StatusCode(StatusCodes.Status202Accepted, result);
    }

    [HttpGet("erp/integration-logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = dbContext.IntegrationSyncLogs.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ProcessedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.SystemName, x.EntityType, x.ExternalId, x.Direction, x.Status,
                x.CorrelationId, x.OperationType, x.HttpStatusCode, x.RetryCount,
                x.ErrorMessage, x.ProcessedAt
            }).ToListAsync(cancellationToken);
        return Ok(new { pageNumber = page, pageSize, totalCount = total, items });
    }
}
