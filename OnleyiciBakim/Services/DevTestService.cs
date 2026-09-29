using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.ViewModels;

namespace OnleyiciBakim.Services;

public interface IDevTestService
{
    Task<DevTestViewModel> GetAsync(CancellationToken cancellationToken);
}

public sealed class DevTestService(
    ApplicationDbContext dbContext,
    IRiskClassificationService classificationService) : IDevTestService
{
    private static readonly decimal[] BoundaryScores = [29.999m, 30m, 59.999m, 60m, 79.999m, 80m, 100m];

    public async Task<DevTestViewModel> GetAsync(CancellationToken cancellationToken)
    {
        var totalMachines = await dbContext.Machines.AsNoTracking().CountAsync(cancellationToken);
        var openFailures = await dbContext.FailureRecords.AsNoTracking()
            .CountAsync(x => x.Status != RecordStatus.Closed && x.Status != RecordStatus.Cancelled,
                cancellationToken);
        var pendingMaintenance = await dbContext.MaintenancePlans.AsNoTracking()
            .CountAsync(x => x.Status != MaintenancePlanStatus.Completed &&
                             x.Status != MaintenancePlanStatus.Cancelled, cancellationToken);
        var groupedRisks = await dbContext.Machines.AsNoTracking()
            .GroupBy(x => x.CurrentRiskLevel)
            .Select(x => new { Level = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);
        var riskCounts = Enum.GetValues<RiskLevel>()
            .Select(level => new DevRiskCount(
                classificationService.GetLabel(level),
                groupedRisks.FirstOrDefault(x => x.Level == level)?.Count ?? 0,
                classificationService.GetColorName(level)))
            .ToArray();
        var rows = await dbContext.Machines.AsNoTracking()
            .OrderByDescending(x => x.CurrentRiskScore)
            .Take(50)
            .Select(x => new { x.Id, x.Code, x.Name, x.CurrentRiskScore, x.CurrentRiskLevel })
            .ToListAsync(cancellationToken);
        var machines = rows.Select(x => new DevMachineItem(
            x.Id, x.Code, x.Name, x.CurrentRiskScore,
            classificationService.GetLabel(x.CurrentRiskLevel))).ToArray();
        var boundaries = BoundaryScores.Select(score =>
        {
            var level = classificationService.Classify(score);
            return new DevBoundaryItem(
                score, classificationService.GetLabel(level), classificationService.GetColorName(level));
        }).ToArray();
        var logs = await dbContext.IntegrationSyncLogs.AsNoTracking()
            .OrderByDescending(x => x.ProcessedAt)
            .Take(20)
            .Select(x => new DevIntegrationLogItem(
                x.Id, x.SystemName, x.EntityType, x.Status.ToString(), x.ProcessedAt))
            .ToListAsync(cancellationToken);
        return new DevTestViewModel(
            totalMachines, openFailures, pendingMaintenance, riskCounts, machines, boundaries, logs);
    }
}
