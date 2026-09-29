using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Integrations;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services;

public sealed class IntegrationService(
    ApplicationDbContext dbContext,
    IOptions<IntegrationOptions> options)
    : IIntegrationService
{
    public async Task<IntegrationResult> SyncMachinesAsync(
        ErpMachineSyncRequest request,
        CancellationToken cancellationToken)
    {
        var duplicateResult = await GetDuplicateResultAsync(
            request.CorrelationId,
            request.Machines.Count,
            cancellationToken);
        if (duplicateResult is not null)
        {
            return duplicateResult;
        }

        var created = 0;
        var updated = 0;
        var errors = new List<string>();
        foreach (var item in request.Machines)
        {
            try
            {
                var machine = await dbContext.Machines.FirstOrDefaultAsync(
                    x => x.Id == item.Id || x.Code == item.Code,
                    cancellationToken);
                if (machine is null)
                {
                    machine = new Machine
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Name = item.Name
                    };
                    dbContext.Machines.Add(machine);
                    created++;
                }
                else
                {
                    updated++;
                }

                machine.ExternalId = item.ExternalId;
                machine.Code = item.Code;
                machine.Name = item.Name;
                machine.CompanyId = item.CompanyId;
                machine.BranchId = item.BranchId;
                machine.DepartmentId = item.DepartmentId;
                machine.ProductionLineId = item.ProductionLineId;
                machine.Type = item.Type;
                machine.Model = item.Model;
                machine.Age = item.Age;
                machine.Criticality = item.Criticality;
                machine.InstallationYear = item.InstallationYear;
                machine.Status = item.Status;
                machine.DataSource = item.DataSource ?? options.Value.ErpSystemName;
            }
            catch (Exception exception)
            {
                errors.Add($"{item.Id}: {exception.Message}");
            }
        }

        var result = new IntegrationResult(
            request.CorrelationId,
            request.Machines.Count,
            created,
            updated,
            errors.Count,
            errors);
        AddLog(request.CorrelationId, "Machine", request, result);
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IntegrationResult> SyncFailuresAsync(
        ErpFailureSyncRequest request,
        CancellationToken cancellationToken)
    {
        var duplicateResult = await GetDuplicateResultAsync(
            request.CorrelationId,
            request.Failures.Count,
            cancellationToken);
        if (duplicateResult is not null)
        {
            return duplicateResult;
        }

        var created = 0;
        var updated = 0;
        var errors = new List<string>();
        foreach (var item in request.Failures)
        {
            var machineExists = await dbContext.Machines.AnyAsync(x => x.Id == item.MachineId, cancellationToken);
            if (!machineExists)
            {
                errors.Add($"{item.Id}: '{item.MachineId}' makinesi bulunamadı.");
                continue;
            }

            var failure = await dbContext.FailureRecords.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
            if (failure is null)
            {
                failure = new FailureRecord { Id = item.Id };
                dbContext.FailureRecords.Add(failure);
                created++;
            }
            else
            {
                updated++;
            }

            failure.MachineId = item.MachineId;
            failure.OccurredAt = item.OccurredAt.ToUniversalTime();
            failure.ComponentCode = item.ComponentCode;
            failure.ComponentName = item.ComponentName;
            failure.FailureType = item.FailureType;
            failure.Severity = item.Severity;
            failure.IsRecurring = item.IsRecurring;
            failure.IsPlannedDowntime = item.IsPlannedDowntime;
            failure.FirstResponseMinutes = item.FirstResponseMinutes;
            failure.InterventionMinutes = item.InterventionMinutes;
            failure.DowntimeMinutes = item.DowntimeMinutes;
            failure.RootCause = item.RootCause;
            failure.Description = item.Description;
            failure.PersonnelId = item.PersonnelId;
            failure.WorkOrderNumber = item.WorkOrderNumber;
            failure.EstimatedCost = item.EstimatedCost;
            failure.DataSource = item.DataSource ?? options.Value.ErpSystemName;
        }

        var result = new IntegrationResult(
            request.CorrelationId,
            request.Failures.Count,
            created,
            updated,
            errors.Count,
            errors);
        AddLog(request.CorrelationId, "FailureRecord", request, result);
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<IntegrationResult?> GetDuplicateResultAsync(
        string correlationId,
        int receivedCount,
        CancellationToken cancellationToken)
    {
        var processed = await dbContext.IntegrationSyncLogs
            .AsNoTracking()
            .AnyAsync(x => x.CorrelationId == correlationId && x.Status == SyncStatus.Processed, cancellationToken);
        return processed
            ? new IntegrationResult(correlationId, receivedCount, 0, 0, 0,
                ["Bu correlationId daha önce işlendi; idempotent yanıt döndürüldü."])
            : null;
    }

    private void AddLog(string correlationId, string entityType, object request, IntegrationResult result)
    {
        dbContext.IntegrationSyncLogs.Add(new IntegrationSyncLog
        {
            SystemName = options.Value.ErpSystemName,
            EntityType = entityType,
            Direction = SyncDirection.Inbound,
            Status = result.RejectedCount == result.ReceivedCount ? SyncStatus.Failed : SyncStatus.Processed,
            CorrelationId = correlationId,
            PayloadJson = JsonSerializer.Serialize(request),
            ErrorMessage = result.Errors.Count == 0 ? null : string.Join(" | ", result.Errors)
        });
    }
}
