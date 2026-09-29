using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Services;

/// <summary>
/// Reads the centrally managed BakimYonetimiDb schema without changing it.
/// </summary>
public sealed class BakimYonetimiDashboardService(
    BakimYonetimiDbContext dbContext,
    IRiskClassificationService classificationService)
    : IDashboardService
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<DashboardResponse> GetAsync(
        int months,
        int riskyMachineCount,
        int? year,
        CancellationToken cancellationToken)
    {
        months = Math.Clamp(months, 1, 24);
        riskyMachineCount = Math.Clamp(riskyMachineCount, 1, 50);

        var now = DateTime.UtcNow;
        var latestFailureQuery = dbContext.ArizaKartlari
            .AsNoTracking()
            .AsQueryable();
        if (year.HasValue)
        {
            var selectedStart = new DateTime(year.Value, 1, 1);
            var selectedEnd = selectedStart.AddYears(1);
            latestFailureQuery = latestFailureQuery.Where(
                x => x.ArizaTarihi >= selectedStart && x.ArizaTarihi < selectedEnd);
        }
        var latestFailure = await latestFailureQuery
            .MaxAsync(x => (DateTime?)x.ArizaTarihi, cancellationToken);

        DateTime periodStart;
        if (year.HasValue && months == 12)
        {
            periodStart = new DateTime(year.Value, 1, 1);
        }
        else
        {
            // Seçilen yıldaki son kayıt, tarihsel yedekteki son kayıt veya güncel ay
            // grafiğin bitiş ayıdır. Böylece 6 aylık görünüm daima altı gerçek ay taşır.
            var fallbackAnchor = year.HasValue
                ? year.Value == now.Year ? now : new DateTime(year.Value, 12, 1)
                : now;
            var chartAnchor = latestFailure ?? fallbackAnchor;
            periodStart = new DateTime(chartAnchor.Year, chartAnchor.Month, 1)
                .AddMonths(-(months - 1));
        }
        var periodEnd = periodStart.AddMonths(months);

        var totalMachines = await dbContext.Makineler.AsNoTracking().CountAsync(cancellationToken);
        var riskyMachines = await dbContext.Makineler.AsNoTracking()
            .CountAsync(x => x.GuncelRiskPuani >= 60m, cancellationToken);
        var pendingPlans = await dbContext.BakimPlanlari.AsNoTracking()
            .CountAsync(x => x.Durum != "Tamamlandı" && x.Durum != "İptal", cancellationToken);
        var averageDowntimeMinutes = await dbContext.ArizaKartlari.AsNoTracking()
            .AverageAsync(x => (decimal?)x.DurusSuresiDk, cancellationToken) ?? 0m;

        var monthlyRaw = await dbContext.ArizaKartlari.AsNoTracking()
            .Where(x => x.ArizaTarihi >= periodStart && x.ArizaTarihi < periodEnd)
            .GroupBy(x => new { x.ArizaTarihi.Year, x.ArizaTarihi.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var monthlyFailures = Enumerable.Range(0, months)
            .Select(offset => periodStart.AddMonths(offset))
            .Select(month => new MonthlyFailurePoint(
                month.Year,
                month.Month,
                TurkishCulture.DateTimeFormat.GetAbbreviatedMonthName(month.Month),
                monthlyRaw.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Count ?? 0))
            .ToArray();

        var failureTypeRows = await dbContext.ArizaKartlari.AsNoTracking()
            .GroupBy(x => x.ArizaTuru ?? "Belirtilmemiş")
            .Select(group => new { Category = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);
        var failureTypes = failureTypeRows
            .Select(x => new CategoryCount(x.Category, x.Count))
            .ToArray();

        var scoreBands = await dbContext.Makineler.AsNoTracking()
            .Select(x => x.GuncelRiskPuani)
            .ToListAsync(cancellationToken);
        var riskDistribution = Enum.GetValues<RiskLevel>()
            .Select(level => new CategoryCount(
                classificationService.GetLabel(level),
                scoreBands.Count(score => classificationService.Classify(score ?? 0m) == level)))
            .ToArray();

        var topRows = await dbContext.Makineler.AsNoTracking()
            .OrderByDescending(x => x.GuncelRiskPuani)
            .Take(riskyMachineCount)
            .Select(x => new
            {
                x.MakineId,
                x.MakineKodu,
                x.MakineAdi,
                Location = x.Hat.HatAdi,
                x.GuncelRiskPuani,
                NextMaintenanceAt = x.BakimPlanlari
                    .Where(plan => plan.Durum != "Tamamlandı" && plan.Durum != "İptal")
                    .OrderBy(plan => plan.PlanlananTarih)
                    .Select(plan => (DateTime?)plan.PlanlananTarih)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var riskiest = topRows.Select(x =>
        {
            var level = classificationService.Classify(x.GuncelRiskPuani ?? 0m);
            return new RiskiestMachineResponse(
                x.MakineId,
                x.MakineKodu ?? x.MakineId,
                x.MakineAdi ?? x.MakineId,
                x.Location,
                ToUtc(x.NextMaintenanceAt),
                PresentRisk(x.GuncelRiskPuani ?? 0m, level));
        }).ToArray();

        return new DashboardResponse(
            totalMachines,
            riskyMachines,
            pendingPlans,
            Math.Round(averageDowntimeMinutes / 60m, 1, MidpointRounding.AwayFromZero),
            monthlyFailures,
            failureTypes,
            riskDistribution,
            riskiest,
            DateTimeOffset.UtcNow);
    }

    private RiskPresentation PresentRisk(decimal score, RiskLevel level) => new(
        score,
        classificationService.GetLabel(level),
        classificationService.GetColorName(level),
        classificationService.GetColorHex(level));

    internal static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    internal static DateTimeOffset? ToUtc(DateTime? value) =>
        value.HasValue ? ToUtc(value.Value) : null;
}
