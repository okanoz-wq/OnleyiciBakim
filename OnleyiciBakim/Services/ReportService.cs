using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Services;

public sealed class ReportService(ApplicationDbContext dbContext) : IReportService
{
    public async Task<FailureAnalysisReport> GetFailureAnalysisAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FailureRecords.AsNoTracking()
            .Where(x => x.OccurredAt >= from && x.OccurredAt <= to);

        var summary = await query.GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Downtime = group.Sum(x => (decimal?)x.DowntimeMinutes) ?? 0,
                Intervention = group.Average(x => (decimal?)x.InterventionMinutes) ?? 0,
                Cost = group.Sum(x => x.EstimatedCost) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var byType = await query.GroupBy(x => x.FailureType)
            .Select(group => new CategoryCount(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
        var bySeverity = await query.GroupBy(x => x.Severity)
            .Select(group => new CategoryCount(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return new FailureAnalysisReport(
            from,
            to,
            summary?.Count ?? 0,
            Math.Round((summary?.Downtime ?? 0) / 60m, 2),
            Math.Round(summary?.Intervention ?? 0, 2),
            summary?.Cost ?? 0,
            byType,
            bySeverity);
    }

    public async Task<MaintenancePerformanceReport> GetMaintenancePerformanceAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var completedQuery = dbContext.MaintenanceRecords.AsNoTracking()
            .Where(x => x.PerformedAt >= from && x.PerformedAt <= to);
        var completed = await completedQuery.CountAsync(cancellationToken);
        var totalCost = await completedQuery.SumAsync(x => x.Cost, cancellationToken) ?? 0m;
        var averageDuration = await completedQuery.AverageAsync(
            x => (decimal?)x.DurationMinutes,
            cancellationToken) ?? 0m;

        var planQuery = dbContext.MaintenancePlans.AsNoTracking()
            .Where(x => x.PlannedAt >= from && x.PlannedAt <= to);
        var planned = await planQuery.CountAsync(cancellationToken);
        var completedPlans = await planQuery.CountAsync(
            x => x.Status == MaintenancePlanStatus.Completed,
            cancellationToken);
        var overdue = await planQuery.CountAsync(
            x => x.Status == MaintenancePlanStatus.Overdue ||
                 (x.PlannedAt < DateTimeOffset.UtcNow &&
                  x.Status == MaintenancePlanStatus.Planned),
            cancellationToken);

        return new MaintenancePerformanceReport(
            from,
            to,
            completed,
            planned,
            planned == 0 ? 0m : Math.Round(completedPlans / (decimal)planned * 100m, 2),
            totalCost,
            Math.Round(averageDuration, 2),
            overdue);
    }

    public async Task<ModelRuleComparisonReport> GetModelRuleComparisonAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.RiskAssessments.AsNoTracking()
            .Where(x => x.EvaluatedAt >= from && x.EvaluatedAt <= to &&
                        x.MachineLearningScore.HasValue)
            .Select(x => new ModelRuleComparisonItem(
                x.Id,
                x.MachineId,
                x.EvaluatedAt,
                x.AlgorithmicScore,
                x.MachineLearningScore!.Value,
                x.HybridScore,
                Math.Abs(x.AlgorithmicScore - x.MachineLearningScore!.Value)))
            .ToListAsync(cancellationToken);

        return new ModelRuleComparisonReport(
            rows.Count,
            rows.Count == 0 ? 0m : Math.Round(rows.Average(x => x.AbsoluteDifference), 2),
            rows.Count == 0 ? 0m : Math.Round(rows.Average(x => x.AlgorithmicScore), 2),
            rows.Count == 0 ? 0m : Math.Round(rows.Average(x => x.MachineLearningScore), 2),
            rows.Count == 0 ? 0m : Math.Round(rows.Average(x => x.HybridScore), 2),
            rows.OrderByDescending(x => x.AbsoluteDifference).Take(20).ToArray());
    }
}
