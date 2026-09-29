using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Services;

public sealed class DashboardService(
    ApplicationDbContext dbContext,
    IRiskClassificationService classificationService)
    : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(
        int months,
        int riskyMachineCount,
        int? year,
        CancellationToken cancellationToken)
    {
        months = Math.Clamp(months, 1, 24);
        riskyMachineCount = Math.Clamp(riskyMachineCount, 1, 50);
        var now = DateTimeOffset.UtcNow;
        var latestFailureQuery = dbContext.FailureRecords.AsNoTracking().AsQueryable();
        if (year.HasValue)
        {
            var selectedStart = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
            latestFailureQuery = latestFailureQuery.Where(
                x => x.OccurredAt >= selectedStart && x.OccurredAt < selectedStart.AddYears(1));
        }
        var latestFailure = await latestFailureQuery
            .MaxAsync(x => (DateTimeOffset?)x.OccurredAt, cancellationToken);
        DateTimeOffset periodStart;
        if (year.HasValue && months == 12)
        {
            periodStart = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }
        else
        {
            var chartAnchor = latestFailure ?? (year.HasValue && year.Value != now.Year
                ? new DateTimeOffset(year.Value, 12, 1, 0, 0, 0, TimeSpan.Zero)
                : now);
            periodStart = new DateTimeOffset(
                chartAnchor.Year, chartAnchor.Month, 1, 0, 0, 0, TimeSpan.Zero)
                .AddMonths(-(months - 1));
        }

        var totalMachines = await dbContext.Machines.CountAsync(cancellationToken);
        var riskyMachines = await dbContext.Machines.CountAsync(
            x => x.CurrentRiskScore >= 60m,
            cancellationToken);
        var pendingPlans = await dbContext.MaintenancePlans.CountAsync(
            x => x.Status == MaintenancePlanStatus.Planned ||
                 x.Status == MaintenancePlanStatus.InProgress ||
                 (x.Status == MaintenancePlanStatus.Overdue),
            cancellationToken);
        var averageDowntimeMinutes = await dbContext.FailureRecords
            .Where(x => x.DowntimeMinutes != null)
            .AverageAsync(x => (decimal?)x.DowntimeMinutes, cancellationToken) ?? 0m;

        var monthlyRaw = await dbContext.FailureRecords
            .Where(x => x.OccurredAt >= periodStart && x.OccurredAt < periodStart.AddMonths(months))
            .GroupBy(x => new { x.OccurredAt.Year, x.OccurredAt.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var monthlyFailures = Enumerable.Range(0, months)
            .Select(offset => periodStart.AddMonths(offset))
            .Select(month =>
            {
                var count = monthlyRaw.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Count ?? 0;
                return new MonthlyFailurePoint(month.Year, month.Month, month.ToString("MMM"), count);
            })
            .ToArray();

        var failureTypes = await dbContext.FailureRecords
            .GroupBy(x => x.FailureType)
            .Select(group => new CategoryCount(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        var riskRaw = await dbContext.Machines
            .GroupBy(x => x.CurrentRiskLevel)
            .Select(group => new { Level = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var riskDistribution = Enum.GetValues<RiskLevel>()
            .Select(level => new CategoryCount(
                classificationService.GetLabel(level),
                riskRaw.FirstOrDefault(x => x.Level == level)?.Count ?? 0))
            .ToArray();

        var topMachines = await dbContext.Machines
            .AsNoTracking()
            .OrderByDescending(x => x.CurrentRiskScore)
            .Take(riskyMachineCount)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                Location = x.ProductionLine != null ? x.ProductionLine.Name : null,
                x.CurrentRiskScore,
                x.CurrentRiskLevel,
                NextMaintenanceAt = x.MaintenancePlans
                    .Where(plan => plan.Status == MaintenancePlanStatus.Planned ||
                                   plan.Status == MaintenancePlanStatus.InProgress)
                    .OrderBy(plan => plan.PlannedAt)
                    .Select(plan => (DateTimeOffset?)plan.PlannedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var riskiest = topMachines.Select(x => new RiskiestMachineResponse(
            x.Id,
            x.Code,
            x.Name,
            x.Location,
            x.NextMaintenanceAt,
            new RiskPresentation(
                x.CurrentRiskScore,
                classificationService.GetLabel(x.CurrentRiskLevel),
                classificationService.GetColorName(x.CurrentRiskLevel),
                classificationService.GetColorHex(x.CurrentRiskLevel))))
            .ToArray();

        return new DashboardResponse(
            totalMachines,
            riskyMachines,
            pendingPlans,
            Math.Round(averageDowntimeMinutes / 60m, 1, MidpointRounding.AwayFromZero),
            monthlyFailures,
            failureTypes,
            riskDistribution,
            riskiest,
            now);
    }
}
