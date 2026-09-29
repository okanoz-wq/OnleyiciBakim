using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Domain.Entities;

public sealed class Machine : BaseEntity
{
    public string Id { get; set; } = null!;
    public string? ExternalId { get; set; }
    public string? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public string? DepartmentId { get; set; }
    public string? ProductionLineId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Type { get; set; }
    public string? Model { get; set; }
    public int? Age { get; set; }
    public string Criticality { get; set; } = "Orta";
    public int? InstallationYear { get; set; }
    public string Status { get; set; } = "Aktif";
    public DateTimeOffset? LastMaintenanceAt { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
    public decimal? WorkingHours { get; set; }
    public decimal? CounterLimitHours { get; set; }
    public decimal CurrentRiskScore { get; set; }
    public RiskLevel CurrentRiskLevel { get; set; } = RiskLevel.Low;
    public string? DataSource { get; set; }

    public Company? Company { get; set; }
    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
    public ProductionLine? ProductionLine { get; set; }
    public ICollection<FailureRecord> Failures { get; set; } = [];
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
    public ICollection<ErrorRecord> ErrorRecords { get; set; } = [];
    public ICollection<DailyTelemetry> DailyTelemetry { get; set; } = [];
    public ICollection<RiskAssessment> RiskAssessments { get; set; } = [];
    public ICollection<MaintenancePlan> MaintenancePlans { get; set; } = [];
    public ICollection<MachineComponent> Components { get; set; } = [];
}

public sealed class MachineComponent : BaseEntity
{
    public string Id { get; set; } = null!;
    public string? MachineId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal? ExpectedLifeHours { get; set; }
    public decimal? UsedLifeHours { get; set; }
    public bool IsActive { get; set; } = true;
    public Machine? Machine { get; set; }
}

public sealed class DailyTelemetry : BaseEntity
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public string MachineId { get; set; } = null!;
    public decimal VoltageAverage { get; set; }
    public decimal VoltageStdDev { get; set; }
    public decimal RotationAverage { get; set; }
    public decimal RotationStdDev { get; set; }
    public decimal PressureAverage { get; set; }
    public decimal PressureStdDev { get; set; }
    public decimal VibrationAverage { get; set; }
    public decimal VibrationStdDev { get; set; }
    public int RecordCount { get; set; }
    public decimal AnomalyScore { get; set; }
    public decimal ProductionIntensityPercent { get; set; }
    public string? DataSource { get; set; }
    public Machine Machine { get; set; } = null!;
}
