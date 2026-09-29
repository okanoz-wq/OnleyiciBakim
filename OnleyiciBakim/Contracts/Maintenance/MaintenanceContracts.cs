using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Contracts.Maintenance;

public sealed class MaintenancePlanQuery : PaginationQuery
{
    public string? MachineId { get; init; }
    public MaintenancePlanStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed class CreateMaintenancePlanRequest
{
    [Required, StringLength(30)]
    public string Id { get; init; } = null!;

    [Required, StringLength(30)]
    public string MachineId { get; init; } = null!;

    public string? ComponentCode { get; init; }
    public string? ComponentName { get; init; }
    public DateTimeOffset PlannedAt { get; init; }
    public string MaintenanceType { get; init; } = "Önleyici";
    public string Priority { get; init; } = "Orta";

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationMinutes { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? EstimatedCost { get; init; }

    public string? ResponsiblePersonnelId { get; init; }
    public string? Justification { get; init; }

    [Range(0, 100)]
    public decimal? ModelConfidence { get; init; }

    public string? Recommendation { get; init; }
    public string? RiskAssessmentId { get; init; }
    public string? ErpWorkOrderNumber { get; init; }
}

public sealed class UpdateMaintenancePlanStatusRequest
{
    public MaintenancePlanStatus Status { get; init; }
    public string? ErpWorkOrderNumber { get; init; }
}

public sealed class CompleteMaintenanceRequest
{
    [Required, StringLength(30)]
    public string MaintenanceRecordId { get; init; } = null!;

    public DateTimeOffset PerformedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? PersonnelId { get; init; }

    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; init; }

    [Required]
    public string Result { get; init; } = null!;

    [Range(0, double.MaxValue)]
    public decimal? Cost { get; init; }

    public DateTimeOffset? NextMaintenanceAt { get; init; }
    public Dictionary<string, string>? ChecklistResults { get; init; }
}

public sealed record MaintenancePlanResponse(
    string Id,
    string MachineId,
    string MachineName,
    DateTimeOffset PlannedAt,
    string MaintenanceType,
    string Priority,
    string Status,
    decimal? ModelConfidence,
    decimal RiskScore,
    string? Recommendation,
    string? Justification,
    string? ResponsiblePersonnel,
    string? ErpWorkOrderNumber);

public sealed record MaintenanceHistoryResponse(
    string Id,
    string MachineId,
    string MachineName,
    DateTimeOffset PerformedAt,
    string MaintenanceType,
    int DurationMinutes,
    string Status,
    string? Personnel,
    decimal? Cost,
    string? Result,
    DateTimeOffset? NextMaintenanceAt);
