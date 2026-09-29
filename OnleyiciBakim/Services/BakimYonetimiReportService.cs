using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Data.BakimYonetimi;

namespace OnleyiciBakim.Services;

/// <summary>Raporları sahte değer üretmeden doğrudan yerel SQL Server işlem tablolarından oluşturur.</summary>
public sealed class BakimYonetimiReportService(BakimYonetimiDbContext db) : IReportService
{
    public async Task<FailureAnalysisReport> GetFailureAnalysisAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token)
    {
        var start = from.UtcDateTime; var end = to.UtcDateTime;
        var query = db.ArizaKartlari.AsNoTracking().Where(x => x.ArizaTarihi >= start && x.ArizaTarihi <= end);
        var summary = await query.GroupBy(_ => 1).Select(g => new
        {
            Count = g.Count(), Downtime = g.Sum(x => (int?)x.DurusSuresiDk) ?? 0,
            Intervention = g.Average(x => (decimal?)x.MudahaleSuresiDk) ?? 0,
            Cost = g.Sum(x => x.TahminiMaliyetTl) ?? 0
        }).FirstOrDefaultAsync(token);
        var byType = await query.GroupBy(x => x.ArizaTuru ?? "Tanımsız").Select(g => new CategoryCount(g.Key, g.Count())).OrderByDescending(x => x.Count).ToListAsync(token);
        var bySeverity = await query.GroupBy(x => x.OnemSeviyesi ?? "Tanımsız").Select(g => new CategoryCount(g.Key, g.Count())).OrderByDescending(x => x.Count).ToListAsync(token);
        return new(from, to, summary?.Count ?? 0, Math.Round((summary?.Downtime ?? 0) / 60m, 2), Math.Round(summary?.Intervention ?? 0, 2), summary?.Cost ?? 0, byType, bySeverity);
    }

    public async Task<MaintenancePerformanceReport> GetMaintenancePerformanceAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token)
    {
        var start = from.UtcDateTime; var end = to.UtcDateTime;
        var records = db.BakimKayitlari.AsNoTracking().Where(x => x.BakimTarihi >= start && x.BakimTarihi <= end);
        var completed = await records.CountAsync(token);
        var cost = await records.SumAsync(x => x.MaliyetTl, token) ?? 0m;
        var duration = await records.AverageAsync(x => (decimal?)x.SureDk, token) ?? 0m;
        var plans = db.BakimPlanlari.AsNoTracking().Where(x => x.PlanlananTarih >= start && x.PlanlananTarih <= end);
        var planned = await plans.CountAsync(token);
        var completedPlans = await plans.CountAsync(x => x.Durum == "Tamamlandı", token);
        var overdue = await plans.CountAsync(x => x.PlanlananTarih < DateTime.UtcNow && x.Durum != "Tamamlandı" && x.Durum != "İptal", token);
        return new(from, to, completed, planned, planned == 0 ? 0m : Math.Round(completedPlans * 100m / planned, 2), cost, Math.Round(duration, 2), overdue);
    }

    public async Task<ModelRuleComparisonReport> GetModelRuleComparisonAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token)
    {
        var start = from.UtcDateTime; var end = to.UtcDateTime;
        var raw = await db.RiskAnalizleri.AsNoTracking().Where(x => x.AnalizTarihi >= start && x.AnalizTarihi <= end && x.AiRiskSkoru.HasValue)
            .Select(x => new { x.AnalizId, x.MakineId, x.AnalizTarihi, x.KuralPuani, Ai = x.AiRiskSkoru!.Value, x.NihaiPuan }).ToListAsync(token);
        var rows = raw
            .GroupBy(x => x.MakineId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.AnalizTarihi).ThenByDescending(x => x.AnalizId).First())
            .Select(x => new ModelRuleComparisonItem(x.AnalizId.ToString(), x.MakineId, new DateTimeOffset(DateTime.SpecifyKind(x.AnalizTarihi, DateTimeKind.Utc)), x.KuralPuani ?? 0m, x.Ai, x.NihaiPuan, Math.Abs((x.KuralPuani ?? 0m) - x.Ai))).ToList();
        return new(rows.Count, rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.AbsoluteDifference), 2), rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.AlgorithmicScore), 2), rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.MachineLearningScore), 2), rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.HybridScore), 2), rows.OrderByDescending(x => x.AbsoluteDifference).Take(20).ToArray());
    }
}
