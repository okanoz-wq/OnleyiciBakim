using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Common;

namespace OnleyiciBakim.Contracts.Machines;

public sealed class MachineQuery : PaginationQuery
{
    public string? Search { get; init; }
    public string? CompanyId { get; init; }
    public string? BranchId { get; init; }
    public string? DepartmentId { get; init; }
    public string? ProductionLineId { get; init; }
    public string? Status { get; init; }
    public string? RiskLevel { get; init; }
    public string SortBy { get; init; } = "riskScore";
    public bool Descending { get; init; } = true;
}

public sealed class CreateMachineRequest
{
    [Required, StringLength(30)]
    public string Id { get; init; } = null!;

    [Required, StringLength(50)]
    public string Code { get; init; } = null!;

    [Required, StringLength(200)]
    public string Name { get; init; } = null!;

    [StringLength(30)]
    public string? ExternalId { get; init; }

    public string? CompanyId { get; init; }
    public string? BranchId { get; init; }
    public string? DepartmentId { get; init; }
    public string? ProductionLineId { get; init; }
    public string? Type { get; init; }
    public string? Model { get; init; }

    [Range(0, 100)]
    public int? Age { get; init; }

    public string Criticality { get; init; } = "Orta";

    [Range(1900, 2200)]
    public int? InstallationYear { get; init; }

    public string Status { get; init; } = "Aktif";
    public string? DataSource { get; init; }
}

public sealed class UpdateMachineRequest
{
    [Required, StringLength(50)]
    public string Code { get; init; } = null!;

    [Required, StringLength(200)]
    public string Name { get; init; } = null!;

    public string? CompanyId { get; init; }
    public string? BranchId { get; init; }
    public string? DepartmentId { get; init; }
    public string? ProductionLineId { get; init; }
    public string? Type { get; init; }
    public string? Model { get; init; }

    [Range(0, 100)]
    public int? Age { get; init; }

    public string Criticality { get; init; } = "Orta";

    [Range(1900, 2200)]
    public int? InstallationYear { get; init; }

    public string Status { get; init; } = "Aktif";
}

public sealed record MachineListItemResponse(
    string Id,
    string Code,
    string Name,
    string? Type,
    string? Location,
    string Status,
    DateTimeOffset? LastMaintenanceAt,
    DateTimeOffset? NextMaintenanceAt,
    int TotalFailureCount,
    RiskPresentation Risk);

public sealed record MachineDetailResponse(
    string Id,
    string Code,
    string Name,
    string? Type,
    string? Model,
    string Criticality,
    string Status,
    int? InstallationYear,
    string? Company,
    string? Branch,
    string? Department,
    string? ProductionLine,
    DateTimeOffset? LastMaintenanceAt,
    DateTimeOffset? RecommendedNextMaintenanceAt,
    int TotalFailureCount,
    RiskPresentation Risk,
    IReadOnlyList<MachineFailureSummary> RecentFailures,
    IReadOnlyList<TelemetryResponse> RecentTelemetry);

public sealed record MachineFailureSummary(
    string Id,
    DateTimeOffset OccurredAt,
    string FailureType,
    string Severity,
    decimal DowntimeHours,
    string? Technician,
    string Status);

public sealed class TelemetryRequest
{
    [Required]
    public DateOnly Date { get; init; }

    public decimal VoltageAverage { get; init; }
    public decimal VoltageStdDev { get; init; }
    public decimal RotationAverage { get; init; }
    public decimal RotationStdDev { get; init; }
    public decimal PressureAverage { get; init; }
    public decimal PressureStdDev { get; init; }
    public decimal VibrationAverage { get; init; }
    public decimal VibrationStdDev { get; init; }

    [Range(0, int.MaxValue)]
    public int RecordCount { get; init; }

    [Range(0, 100)]
    public decimal AnomalyScore { get; init; }

    [Range(0, 100)]
    public decimal ProductionIntensityPercent { get; init; }
    public string? DataSource { get; init; }
}

public sealed record TelemetryResponse(
    DateOnly Date,
    decimal VoltageAverage,
    decimal RotationAverage,
    decimal PressureAverage,
    decimal VibrationAverage,
    decimal AnomalyScore,
    decimal ProductionIntensityPercent);
