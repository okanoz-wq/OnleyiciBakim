using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Services;
using OnleyiciBakim.Services.Management;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/local-workflows")]
public sealed class LocalWorkflowsController(ILocalWorkflowService workflows, ILocalRiskAssessmentService risk, ILogger<LocalWorkflowsController> logger) : ControllerBase
{
    [HttpPost("meter-readings")] public async Task<ActionResult<WorkflowResult>> Meter(MeterReadingRequest r, CancellationToken t) { var result = await workflows.AddMeterReadingAsync(r, t); await Recalculate(r.MachineId, "Sayaç verisi güncellendi", t); return Ok(result); }
    [HttpPost("work-calendar")] public async Task<ActionResult<WorkflowResult>> Calendar(WorkCalendarRequest r, CancellationToken t) => Ok(await workflows.AddCalendarAsync(r, t));
    [HttpPost("transfers")] public async Task<ActionResult<WorkflowResult>> Transfer(MachineTransferRequest r, CancellationToken t) => Ok(await workflows.TransferMachineAsync(r, t));
    [HttpPost("failures")] public async Task<ActionResult<WorkflowResult>> Failure(FailureReportRequest r, CancellationToken t) { var result = await workflows.ReportFailureAsync(r, t); await Recalculate(r.MachineId, "Arıza bildirildi", t); return Ok(result); }
    [HttpPost("interventions")] public async Task<ActionResult<WorkflowResult>> Intervention(FailureInterventionRequest r, CancellationToken t) { var result = await workflows.AddInterventionAsync(r, t); if (result.MachineId is not null) await Recalculate(result.MachineId, "Arıza müdahalesi güncellendi", t); return Ok(result); }
    [HttpPost("failures/close")] public async Task<ActionResult<WorkflowResult>> Close(FailureCloseRequest r, CancellationToken t) { var result = await workflows.CloseFailureAsync(r, t); if (result.MachineId is not null) await Recalculate(result.MachineId, "Arıza kapatıldı", t); return Ok(result); }
    [HttpPost("maintenance-executions")] public async Task<ActionResult<WorkflowResult>> Maintenance(MaintenanceExecutionRequest r, CancellationToken t) { var result = await workflows.CompleteMaintenanceAsync(r, t); if (result.MachineId is not null) await Recalculate(result.MachineId, "Bakım tamamlandı", t); return Ok(result); }
    [HttpPost("work-orders")] public async Task<ActionResult<WorkflowResult>> WorkOrder(WorkOrderRequest r, CancellationToken t) => Ok(await workflows.CreateWorkOrderAsync(r, t));
    [HttpPost("work-orders/{id:int}/status")] public async Task<ActionResult<WorkflowResult>> WorkOrderStatus(int id, StatusChangeRequest r, CancellationToken t) => Ok(await workflows.ChangeWorkOrderStatusAsync(id, r, t));
    [HttpPost("warnings/{id}/actions")] public async Task<ActionResult<WorkflowResult>> Warning(string id, WarningActionRequest r, CancellationToken t) => Ok(await workflows.ProcessWarningAsync(id, r, t));

    private async Task Recalculate(string machineId, string reason, CancellationToken token)
    {
        try { await risk.EvaluateMachineAsync(machineId, reason, token); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Veri kaydedildi fakat otomatik risk analizi tamamlanamadı. MachineId={MachineId}", machineId); }
    }
}
