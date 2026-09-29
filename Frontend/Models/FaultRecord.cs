namespace OnleyiciBakimSistemi.Models;

public class FaultRecord
{
    public string Id { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FaultType { get; set; } = string.Empty;

    // Düşük, Orta, Yüksek, Kritik
    public string Severity { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal DowntimeHours { get; set; }
    public string Technician { get; set; } = string.Empty;
    public bool Resolved { get; set; }
}
