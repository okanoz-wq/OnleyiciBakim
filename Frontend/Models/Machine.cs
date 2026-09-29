namespace OnleyiciBakimSistemi.Models;

public class Machine
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    // Normal, İzlemede, Riskli
    public string Status { get; set; } = string.Empty;

    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RiskColorKey { get; set; } = "green";
    public DateTime InstallDate { get; set; }
    public DateTime LastMaintenanceDate { get; set; }
    public DateTime NextMaintenanceDate { get; set; }
    public int TotalFaultCount { get; set; }
}
