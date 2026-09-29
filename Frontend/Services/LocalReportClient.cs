using System.Globalization;
using System.Net.Http.Json;
using OnleyiciBakimSistemi.Models.ViewModels;

namespace OnleyiciBakimSistemi.Services;

public interface ILocalReportClient { Task<OperationalReportViewModel?> GetAsync(string module, CancellationToken token); }

public sealed class LocalReportClient(HttpClient http) : ILocalReportClient
{
    public async Task<OperationalReportViewModel?> GetAsync(string module, CancellationToken token)
    {
        if (module == "failure-reporting")
        {
            var x = await http.GetFromJsonAsync<FailureReport>("api/v1/reports/failure-analysis", token) ?? throw new InvalidOperationException("Rapor alınamadı.");
            return new() { Metrics = new Dictionary<string, string> { ["Toplam Arıza"] = x.FailureCount.ToString(), ["Toplam Duruş"] = x.TotalDowntimeHours.ToString("0.##") + " saat", ["Ort. Müdahale"] = x.AverageInterventionMinutes.ToString("0.##") + " dk", ["Tahmini Maliyet"] = x.TotalEstimatedCost.ToString("N2") + " ₺" }, Rows = x.ByType.Select(y => (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["Arıza Türü"] = y.Category, ["Adet"] = y.Count.ToString() }).ToList() };
        }
        if (module == "maintenance-reporting")
        {
            var x = await http.GetFromJsonAsync<MaintenanceReport>("api/v1/reports/maintenance-performance", token) ?? throw new InvalidOperationException("Rapor alınamadı.");
            return new() { Metrics = new Dictionary<string, string> { ["Gerçekleşen"] = x.CompletedCount.ToString(), ["Planlanan"] = x.PlannedCount.ToString(), ["Tamamlama Oranı"] = "%" + x.CompletionRatePercent.ToString("0.##"), ["Geciken"] = x.OverdueCount.ToString(), ["Ort. Süre"] = x.AverageDurationMinutes.ToString("0.##") + " dk", ["Toplam Maliyet"] = x.TotalCost.ToString("N2") + " ₺" } };
        }
        if (module == "model-comparison")
        {
            var x = await http.GetFromJsonAsync<ModelReport>("api/v1/reports/model-rule-comparison", token) ?? throw new InvalidOperationException("Rapor alınamadı.");
            return new() { Metrics = new Dictionary<string, string> { ["Örnek"] = x.SampleCount.ToString(), ["Ortalama Fark"] = x.MeanAbsoluteDifference.ToString("0.##"), ["Kural Ort."] = x.AverageAlgorithmicScore.ToString("0.##"), ["AI Ort."] = x.AverageMachineLearningScore.ToString("0.##"), ["Hibrit Ort."] = x.AverageHybridScore.ToString("0.##") }, Rows = x.LargestDifferences.Select(y => (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["Makine"] = y.MachineId, ["Tarih"] = y.EvaluatedAt.LocalDateTime.ToString("g", CultureInfo.GetCultureInfo("tr-TR")), ["Kural"] = y.AlgorithmicScore.ToString("0.##"), ["AI"] = y.MachineLearningScore.ToString("0.##"), ["Nihai"] = y.HybridScore.ToString("0.##"), ["Fark"] = y.AbsoluteDifference.ToString("0.##") }).ToList() };
        }
        return null;
    }
    private sealed record CategoryItem(string Category, int Count);
    private sealed record FailureReport(int FailureCount, decimal TotalDowntimeHours, decimal AverageInterventionMinutes, decimal TotalEstimatedCost, List<CategoryItem> ByType, List<CategoryItem> BySeverity);
    private sealed record MaintenanceReport(int CompletedCount, int PlannedCount, decimal CompletionRatePercent, decimal TotalCost, decimal AverageDurationMinutes, int OverdueCount);
    private sealed record ModelRow(string MachineId, DateTimeOffset EvaluatedAt, decimal AlgorithmicScore, decimal MachineLearningScore, decimal HybridScore, decimal AbsoluteDifference);
    private sealed record ModelReport(int SampleCount, decimal MeanAbsoluteDifference, decimal AverageAlgorithmicScore, decimal AverageMachineLearningScore, decimal AverageHybridScore, List<ModelRow> LargestDifferences);
}
