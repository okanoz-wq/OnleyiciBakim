namespace OnleyiciBakimSistemi.Models;

public class MaintenancePlanItem
{
    public string Id { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime PlannedDate { get; set; }

    // Önleyici, Acil
    public string Type { get; set; } = string.Empty;

    public decimal PredictedRiskScore { get; set; }

    // Modelin tahmine olan güveni (%)
    public decimal? Confidence { get; set; }

    // Planlandı, Devam Ediyor, Tamamlandı
    public string Status { get; set; } = string.Empty;

    public string RecommendedAction { get; set; } = string.Empty;
}
